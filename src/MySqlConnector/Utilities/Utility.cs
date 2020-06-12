using System;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MySqlConnector.Utilities
{
	internal static class Utility
	{
		public static void Dispose<T>(ref T? disposable)
			where T : class, IDisposable
		{
			if (disposable is not null)
			{
				disposable.Dispose();
				disposable = null;
			}
		}

		public static string FormatInvariant(this string format, params object?[] args) =>
			string.Format(CultureInfo.InvariantCulture, format, args);

#if NET45 || NET461 || NET471 || NETSTANDARD1_3 || NETSTANDARD2_0
		public static string GetString(this Encoding encoding, ReadOnlySpan<byte> span)
		{
			if (span.Length == 0)
				return "";
#if NET45
			return encoding.GetString(span.ToArray());
#else
			unsafe
			{
				fixed (byte* ptr = &MemoryMarshal.GetReference(span))
					return encoding.GetString(ptr, span.Length);
			}
#endif
		}

		public static unsafe int GetByteCount(this Encoding encoding, ReadOnlySpan<char> chars)
		{
			if (chars.Length == 0)
				return 0;

			fixed (char* charsPtr = &MemoryMarshal.GetReference(chars))
			{
				return encoding.GetByteCount(charsPtr, chars.Length);
			}
		}

		public static unsafe int GetBytes(this Encoding encoding, ReadOnlySpan<char> chars, Span<byte> bytes)
		{
			fixed (char* charsPtr = &MemoryMarshal.GetReference(chars))
			fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bytes))
			{
				return encoding.GetBytes(charsPtr, chars.Length, bytesPtr, bytes.Length);
			}
		}
#endif

#if NET461 || NET471 || NETSTANDARD2_0
		public static unsafe void Convert(this Encoder encoder, ReadOnlySpan<char> chars, Span<byte> bytes, bool flush, out int charsUsed, out int bytesUsed, out bool completed)
		{
			fixed (char* charsPtr = &MemoryMarshal.GetReference(chars))
			fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bytes))
			{
				// MemoryMarshal.GetNonNullPinnableReference is internal, so fake it by using an invalid but non-null pointer; this
				// prevents Convert from throwing an exception when the output buffer is empty
				encoder.Convert(charsPtr, chars.Length, bytesPtr is null ? (byte*) 1 : bytesPtr, bytes.Length, flush, out charsUsed, out bytesUsed, out completed);
			}
		}
#endif

		/// <summary>
		/// Loads a RSA key from a PEM string.
		/// </summary>
		/// <param name="key">The key, in PEM format.</param>
		/// <returns>An RSA key.</returns>
		public static RSAParameters GetRsaParameters(string key)
		{
			bool isPrivate;
			if (key.StartsWith("-----BEGIN RSA PRIVATE KEY-----", StringComparison.Ordinal))
			{
				key = key.Replace("-----BEGIN RSA PRIVATE KEY-----", "").Replace("-----END RSA PRIVATE KEY-----", "");
				isPrivate = true;
			}
			else if (key.StartsWith("-----BEGIN PUBLIC KEY-----", StringComparison.Ordinal))
			{
				key = key.Replace("-----BEGIN PUBLIC KEY-----", "").Replace("-----END PUBLIC KEY-----", "");
				isPrivate = false;
			}
			else
			{
				throw new FormatException("Unrecognized PEM header: " + key.Substring(0, Math.Min(key.Length, 80)));
			}

			return GetRsaParameters(System.Convert.FromBase64String(key), isPrivate);
		}

		// Derived from: https://stackoverflow.com/a/32243171/, https://stackoverflow.com/a/26978561/, http://luca.ntop.org/Teaching/Appunti/asn1.html
		private static RSAParameters GetRsaParameters(ReadOnlySpan<byte> data, bool isPrivate)
		{
			// read header (30 81 xx, or 30 82 xx xx)
			if (data[0] != 0x30)
				throw new FormatException("Expected 0x30 but read {0:X2}".FormatInvariant(data[0]));
			data = data.Slice(1);

			if (!TryReadAsnLength(data, out var length, out var bytesConsumed))
				throw new FormatException("Couldn't read key length");
			data = data.Slice(bytesConsumed);

			if (!isPrivate)
			{
				if (!data.Slice(0, s_rsaOid.Length).SequenceEqual(s_rsaOid))
					throw new FormatException("Expected RSA OID but read {0}".FormatInvariant(BitConverter.ToString(data.Slice(0, 15).ToArray())));
				data = data.Slice(s_rsaOid.Length);

				// BIT STRING (0x03) followed by length
				if (data[0] != 0x03)
					throw new FormatException("Expected 0x03 but read {0:X2}".FormatInvariant(data[0]));
				data = data.Slice(1);

				if (!TryReadAsnLength(data, out length, out bytesConsumed))
					throw new FormatException("Couldn't read length");
				data = data.Slice(bytesConsumed);

				// skip NULL byte
				if (data[0] != 0x00)
					throw new FormatException("Expected 0x00 but read {0:X2}".FormatInvariant(data[0]));
				data = data.Slice(1);

				// skip next header (30 81 xx, or 30 82 xx xx)
				if (data[0] != 0x30)
					throw new FormatException("Expected 0x30 but read {0:X2}".FormatInvariant(data[0]));
				data = data.Slice(1);

				if (!TryReadAsnLength(data, out length, out bytesConsumed))
					throw new FormatException("Couldn't read length");
				data = data.Slice(bytesConsumed);
			}
			else
			{
				if (!TryReadAsnInteger(data, out var zero, out bytesConsumed) || zero.Length != 1 || zero[0] != 0)
					throw new FormatException("Couldn't read zero.");
				data = data.Slice(bytesConsumed);
			}

			if (!TryReadAsnInteger(data, out var modulus, out bytesConsumed))
				throw new FormatException("Couldn't read modulus");
			data = data.Slice(bytesConsumed);

			if (!TryReadAsnInteger(data, out var exponent, out bytesConsumed))
				throw new FormatException("Couldn't read exponent");
			data = data.Slice(bytesConsumed);

			if (!isPrivate)
			{
				return new RSAParameters
				{
					Modulus = modulus.ToArray(),
					Exponent = exponent.ToArray(),
				};
			}

			if (!TryReadAsnInteger(data, out var d, out bytesConsumed))
				throw new FormatException("Couldn't read D");
			data = data.Slice(bytesConsumed);

			if (!TryReadAsnInteger(data, out var p, out bytesConsumed))
				throw new FormatException("Couldn't read P");
			data = data.Slice(bytesConsumed);

			if (!TryReadAsnInteger(data, out var q, out bytesConsumed))
				throw new FormatException("Couldn't read Q");
			data = data.Slice(bytesConsumed);

			if (!TryReadAsnInteger(data, out var dp, out bytesConsumed))
				throw new FormatException("Couldn't read DP");
			data = data.Slice(bytesConsumed);

			if (!TryReadAsnInteger(data, out var dq, out bytesConsumed))
				throw new FormatException("Couldn't read DQ");
			data = data.Slice(bytesConsumed);

			if (!TryReadAsnInteger(data, out var iq, out bytesConsumed))
				throw new FormatException("Couldn't read IQ");
			data = data.Slice(bytesConsumed);

			return new RSAParameters
			{
				Modulus = modulus.ToArray(),
				Exponent = exponent.ToArray(),
				D = d.ToArray(),
				P = p.ToArray(),
				Q = q.ToArray(),
				DP = dp.ToArray(),
				DQ = dq.ToArray(),
				InverseQ = iq.ToArray(),
			};
		}

		/// <summary>
		/// Returns a new <see cref="ArraySegment{T}"/> that starts at index <paramref name="index"/> into <paramref name="arraySegment"/>.
		/// </summary>
		/// <param name="arraySegment">The <see cref="ArraySegment{T}"/> from which to create a slice.</param>
		/// <param name="index">The non-negative, zero-based starting index of the new slice (relative to <see cref="ArraySegment{T}.Offset"/> of <paramref name="arraySegment"/>.</param>
		/// <returns>A new <see cref="ArraySegment{T}"/> starting at the <paramref name="index"/>th element of <paramref name="arraySegment"/> and continuing to the end of <paramref name="arraySegment"/>.</returns>
		public static ArraySegment<T> Slice<T>(this ArraySegment<T> arraySegment, int index) =>
			new ArraySegment<T>(arraySegment.Array!, arraySegment.Offset + index, arraySegment.Count - index);

		/// <summary>
		/// Returns a new <see cref="ArraySegment{T}"/> that starts at index <paramref name="index"/> into <paramref name="arraySegment"/> and has a length of <paramref name="length"/>.
		/// </summary>
		/// <param name="arraySegment">The <see cref="ArraySegment{T}"/> from which to create a slice.</param>
		/// <param name="index">The non-negative, zero-based starting index of the new slice (relative to <see cref="ArraySegment{T}.Offset"/> of <paramref name="arraySegment"/>.</param>
		/// <param name="length">The non-negative length of the new slice.</param>
		/// <returns>A new <see cref="ArraySegment{T}"/> of length <paramref name="length"/>, starting at the <paramref name="index"/>th element of <paramref name="arraySegment"/>.</returns>
		public static ArraySegment<T> Slice<T>(this ArraySegment<T> arraySegment, int index, int length) =>
			new ArraySegment<T>(arraySegment.Array!, arraySegment.Offset + index, length);

		/// <summary>
		/// Returns a new <see cref="T:byte[]"/> that is a slice of <paramref name="input"/> starting at <paramref name="offset"/>.
		/// </summary>
		/// <param name="input">The array to slice.</param>
		/// <param name="offset">The offset at which to slice.</param>
		/// <param name="length">The length of the slice.</param>
		/// <returns>A new <see cref="T:byte[]"/> that is a slice of <paramref name="input"/> from <paramref name="offset"/> to the end.</returns>
		public static byte[] ArraySlice(byte[] input, int offset, int length)
		{
			if (offset == 0 && length == input.Length)
				return input;
			var slice = new byte[length];
			Array.Copy(input, offset, slice, 0, slice.Length);
			return slice;
		}

		/// <summary>
		/// Finds the next index of <paramref name="pattern"/> in <paramref name="data"/>, starting at index <paramref name="offset"/>.
		/// </summary>
		/// <param name="data">The array to search.</param>
		/// <param name="offset">The offset at which to start searching.</param>
		/// <param name="pattern">The pattern to find in <paramref name="data"/>.</param>
		/// <returns>The offset of <paramref name="pattern"/> within <paramref name="data"/>, or <c>-1</c> if <paramref name="pattern"/> was not found.</returns>
		public static int FindNextIndex(ReadOnlySpan<byte> data, int offset, ReadOnlySpan<byte> pattern)
		{
			var limit = data.Length - pattern.Length;
			for (var start = offset; start <= limit; start++)
			{
				var i = 0;
				for (; i < pattern.Length; i++)
				{
					if (data[start + i] != pattern[i])
						break;
				}
				if (i == pattern.Length)
					return start;
			}
			return -1;
		}

		/// <summary>
		/// Resizes <paramref name="resizableArray"/> to hold at least <paramref name="newLength"/> items.
		/// </summary>
		/// <remarks><paramref name="resizableArray"/> may be <c>null</c>, in which case a new <see cref="ResizableArray{T}"/> will be allocated.</remarks>
		public static void Resize<T>([NotNull] ref ResizableArray<T>? resizableArray, int newLength)
			where T : notnull
		{
			resizableArray ??= new ResizableArray<T>();
			resizableArray.DoResize(newLength);
		}

		public static TimeSpan ParseTimeSpan(ReadOnlySpan<byte> value)
		{
			var originalValue = value;

			// parse (optional) leading minus sign
			var isNegative = false;
			if (value.Length > 0 && value[0] == 0x2D)
			{
				isNegative = true;
				value = value.Slice(1);
			}

			// parse hours (0-838)
			if (!Utf8Parser.TryParse(value, out int hours, out var bytesConsumed) || hours < 0 || hours > 838)
				goto InvalidTimeSpan;
			if (value.Length == bytesConsumed || value[bytesConsumed] != 58)
				goto InvalidTimeSpan;
			value = value.Slice(bytesConsumed + 1);

			// parse minutes (0-59)
			if (!Utf8Parser.TryParse(value, out int minutes, out bytesConsumed) || bytesConsumed != 2 || minutes < 0 || minutes > 59)
				goto InvalidTimeSpan;
			if (value.Length < 3 || value[2] != 58)
				goto InvalidTimeSpan;
			value = value.Slice(3);

			// parse seconds (0-59)
			if (!Utf8Parser.TryParse(value, out int seconds, out bytesConsumed) || bytesConsumed != 2 || seconds < 0 || seconds > 59)
				goto InvalidTimeSpan;

			int microseconds;
			if (value.Length == 2)
			{
				microseconds = 0;
			}
			else
			{
				if (value[2] != 46)
					goto InvalidTimeSpan;
				value = value.Slice(3);
				if (!Utf8Parser.TryParse(value, out microseconds, out bytesConsumed) || bytesConsumed != value.Length || microseconds < 0 || microseconds > 999_999)
					goto InvalidTimeSpan;
				for (; bytesConsumed < 6; bytesConsumed++)
					microseconds *= 10;
			}

			if (isNegative)
			{
				hours = -hours;
				minutes = -minutes;
				seconds = -seconds;
				microseconds = -microseconds;
			}
			return new TimeSpan(0, hours, minutes, seconds, microseconds / 1000) + TimeSpan.FromTicks(microseconds % 1000 * 10);

			InvalidTimeSpan:
			throw new FormatException("Couldn't interpret '{0}' as a valid TimeSpan".FormatInvariant(Encoding.UTF8.GetString(originalValue)));
		}

#if NET45
		public static Task CompletedTask
		{
			get
			{
				if (s_completedTask is null)
				{
					var tcs = new TaskCompletionSource<object>();
					tcs.SetResult(tcs);
					s_completedTask = tcs.Task;
				}
				return s_completedTask;
			}
		}
		static Task? s_completedTask;

		public static Task TaskFromException(Exception exception) => TaskFromException<object>(exception);
		public static Task<T> TaskFromException<T>(Exception exception)
		{
			var tcs = new TaskCompletionSource<T>();
			tcs.SetException(exception);
			return tcs.Task;
		}

		public static byte[] EmptyByteArray { get; } = new byte[0];
#else
		public static Task CompletedTask => Task.CompletedTask;
		public static Task TaskFromException(Exception exception) => Task.FromException(exception);
		public static Task<T> TaskFromException<T>(Exception exception) => Task.FromException<T>(exception);
		public static byte[] EmptyByteArray { get; } = Array.Empty<byte>();
#endif

#if NET45 || NET461 || NET471 || NETSTANDARD1_3 || NETSTANDARD2_0
		public static bool TryComputeHash(this HashAlgorithm hashAlgorithm, ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
		{
			// assume caller supplies a large-enough buffer so we don't have to bounds-check it
			var output = hashAlgorithm.ComputeHash(source.ToArray());
			output.AsSpan().CopyTo(destination);
			bytesWritten = output.Length;
			return true;
		}
#endif

#if !NETSTANDARD2_1 && !NETCOREAPP3_0
		public static Task CompletedValueTask => CompletedTask;
#else
		public static ValueTask CompletedValueTask => default;
#endif

		public static byte[] TrimZeroByte(byte[] value)
		{
			if (value[value.Length - 1] == 0)
				Array.Resize(ref value, value.Length - 1);
			return value;
		}

		public static ReadOnlySpan<byte> TrimZeroByte(ReadOnlySpan<byte> value) =>
			value[value.Length - 1] == 0 ? value.Slice(0, value.Length - 1) : value;

#if NET45
		public static bool TryGetBuffer(this MemoryStream memoryStream, out ArraySegment<byte> buffer)
		{
			try
			{
				var rawBuffer = memoryStream.GetBuffer();
				buffer = new ArraySegment<byte>(rawBuffer, 0, checked((int) memoryStream.Length));
				return true;
			}
			catch (UnauthorizedAccessException)
			{
				buffer = default(ArraySegment<byte>);
				return false;
			}
		}
#endif

#if !NETSTANDARD2_1 && !NETCOREAPP2_1 && !NETCOREAPP3_0
		public static int Read(this Stream stream, Memory<byte> buffer)
		{
			MemoryMarshal.TryGetArray<byte>(buffer, out var arraySegment);
			return stream.Read(arraySegment.Array, arraySegment.Offset, arraySegment.Count);
		}

		public static Task<int> ReadAsync(this Stream stream, Memory<byte> buffer)
		{
			MemoryMarshal.TryGetArray<byte>(buffer, out var arraySegment);
			return stream.ReadAsync(arraySegment.Array, arraySegment.Offset, arraySegment.Count);
		}

		public static void Write(this Stream stream, ReadOnlyMemory<byte> data)
		{
			MemoryMarshal.TryGetArray(data, out var arraySegment);
			stream.Write(arraySegment.Array, arraySegment.Offset, arraySegment.Count);
		}

		public static Task WriteAsync(this Stream stream, ReadOnlyMemory<byte> data)
		{
			MemoryMarshal.TryGetArray(data, out var arraySegment);
			return stream.WriteAsync(arraySegment.Array, arraySegment.Offset, arraySegment.Count);
		}
#else
		public static int Read(this Stream stream, Memory<byte> buffer) => stream.Read(buffer.Span);

		public static void Write(this Stream stream, ReadOnlyMemory<byte> data) => stream.Write(data.Span);
#endif

		public static void SwapBytes(byte[] bytes, int offset1, int offset2)
		{
			byte swap = bytes[offset1];
			bytes[offset1] = bytes[offset2];
			bytes[offset2] = swap;
		}

#if NET45 || NET461
		public static bool IsWindows() => Environment.OSVersion.Platform == PlatformID.Win32NT;

		public static void GetOSDetails(out string? os, out string osDescription, out string architecture)
		{
			os = Environment.OSVersion.Platform == PlatformID.Win32NT ? "Windows" :
				Environment.OSVersion.Platform == PlatformID.Unix ? "Linux" :
				Environment.OSVersion.Platform == PlatformID.MacOSX ? "macOS" : null;
			osDescription = Environment.OSVersion.VersionString;
			architecture = IntPtr.Size == 8 ? "X64" : "X86";
		}
#else
		public static bool IsWindows()
		{
			try
			{
				// OSPlatform.Windows is not supported on AWS Lambda
				return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
			}
			catch (PlatformNotSupportedException)
			{
				return false;
			}
		}

		public static void GetOSDetails(out string? os, out string osDescription, out string architecture)
		{
			os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" :
				RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux" :
				RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS" : null;
			osDescription = RuntimeInformation.OSDescription;
			architecture = RuntimeInformation.ProcessArchitecture.ToString();
		}
#endif

#if NET45 || NET461
		public static SslProtocols GetDefaultSslProtocols()
		{
			if (!s_defaultSslProtocols.HasValue)
			{
				// Prior to .NET Framework 4.7, SslProtocols.None is not a valid argument to SslStream.AuthenticateAsClientAsync.
				// If the NET461 build is loaded by an application that targets. NET 4.7 (or later), or if app.config has set
				// Switch.System.Net.DontEnableSystemDefaultTlsVersions to false, then SslProtocols.None will work; otherwise,
				// if the application targets .NET 4.6.2 or earlier and hasn't changed the AppContext switch, then it will
				// fail at runtime. We attempt to determine if it will fail by accessing the internal static
				// ServicePointManager.DisableSystemDefaultTlsVersions property, which controls whether SslProtocols.None is
				// an acceptable value.
				bool disableSystemDefaultTlsVersions;
				try
				{
					var property = typeof(ServicePointManager).GetProperty("DisableSystemDefaultTlsVersions", BindingFlags.NonPublic | BindingFlags.Static);
					disableSystemDefaultTlsVersions = property is null || (property.GetValue(null) is bool b && b);
				}
				catch (Exception)
				{
					// couldn't access the property; assume the safer default of 'true'
					disableSystemDefaultTlsVersions = true;
				}

				s_defaultSslProtocols = disableSystemDefaultTlsVersions ? (SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12) : SslProtocols.None;
			}

			return s_defaultSslProtocols.Value;
		}

		static SslProtocols? s_defaultSslProtocols;
#elif NETSTANDARD1_3
		public static SslProtocols GetDefaultSslProtocols() => SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
#else
		public static SslProtocols GetDefaultSslProtocols() => SslProtocols.None;
#endif

		// Reads a length encoded according to ASN.1 BER rules.
		private static bool TryReadAsnLength(ReadOnlySpan<byte> data, out int length, out int bytesConsumed)
		{
			var leadByte = data[0];
			if (leadByte < 0x80)
			{
				// Short form. One octet. Bit 8 has value "0" and bits 7-1 give the length.
				length = leadByte;
				bytesConsumed = 1;
				return true;
			}

			// Long form. Two to 127 octets. Bit 8 of first octet has value "1" and bits 7-1 give the number of additional length octets. Second and following octets give the length, base 256, most significant digit first.
			if (leadByte == 0x81)
			{
				length = data[1];
				bytesConsumed = 2;
				return true;
			}

			if (leadByte == 0x82)
			{
				length = data[1] * 256 + data[2];
				bytesConsumed = 3;
				return true;
			}

			// lengths over 2^16 are not currently handled
			length = 0;
			bytesConsumed = 0;
			return false;
		}

		private static bool TryReadAsnInteger(ReadOnlySpan<byte> data, out ReadOnlySpan<byte> number, out int bytesConsumed)
		{
			// integer tag is 2
			if (data[0] != 0x02)
			{
				number = default;
				bytesConsumed = 0;
				return false;
			}
			data = data.Slice(1);

			// tag is followed by the length of the integer
			if (!TryReadAsnLength(data, out var length, out var lengthBytesConsumed))
			{
				number = default;
				bytesConsumed = 0;
				return false;
			}

			// length is followed by the integer bytes, MSB first
			number = data.Slice(lengthBytesConsumed, length);
			bytesConsumed = lengthBytesConsumed + length + 1;

			// trim leading zero bytes
			while (number.Length > 1 && number[0] == 0)
				number = number.Slice(1);

			return true;
		}

		// encoded OID sequence for  PKCS #1 rsaEncryption szOID_RSA_RSA = "1.2.840.113549.1.1.1"
		private static ReadOnlySpan<byte> s_rsaOid => new byte[] { 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00 };
	}
}
