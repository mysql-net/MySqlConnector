using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
#if NETSTANDARD1_3 || NETSTANDARD2_0
using System.Runtime.InteropServices;
#endif
#if NET45 || NET46
using System.Reflection;
#endif
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MySqlConnector.Utilities
{
	internal static class Utility
	{
		public static void Dispose<T>(ref T disposable)
			where T : class, IDisposable
		{
			if (disposable != null)
			{
				disposable.Dispose();
				disposable = null;
			}
		}

		public static string FormatInvariant(this string format, params object[] args) =>
			string.Format(CultureInfo.InvariantCulture, format, args);

		public static string GetString(this Encoding encoding, ArraySegment<byte> arraySegment) =>
			encoding.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

		/// <summary>
		/// Loads a RSA public key from a PEM string. Taken from <a href="https://stackoverflow.com/a/32243171/23633">Stack Overflow</a>.
		/// </summary>
		/// <param name="publicKey">The public key, in PEM format.</param>
		/// <returns>An RSA public key, or <c>null</c> on failure.</returns>
		public static RSA DecodeX509PublicKey(string publicKey)
		{
			var x509Key = Convert.FromBase64String(publicKey.Replace("-----BEGIN PUBLIC KEY-----", "").Replace("-----END PUBLIC KEY-----", ""));

			// encoded OID sequence for  PKCS #1 rsaEncryption szOID_RSA_RSA = "1.2.840.113549.1.1.1"
			byte[] seqOid = { 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00 };

			// ---------  Set up stream to read the asn.1 encoded SubjectPublicKeyInfo blob  ------
			using (var stream = new MemoryStream(x509Key))
			using (var reader = new BinaryReader(stream)) //wrap Memory Stream with BinaryReader for easy reading
			{
				var temp = reader.ReadUInt16();
				switch (temp)
				{
				case 0x8130:
					reader.ReadByte(); //advance 1 byte
					break;
				case 0x8230:
					reader.ReadInt16(); //advance 2 bytes
					break;
				default:
					throw new FormatException("Expected 0x8130 or 0x8230 but read {0:X4}".FormatInvariant(temp));
				}

				var seq = reader.ReadBytes(15);
				if (!seq.SequenceEqual(seqOid)) //make sure Sequence for OID is correct
					throw new FormatException("Expected RSA OID but read {0}".FormatInvariant(BitConverter.ToString(seq)));

				temp = reader.ReadUInt16();
				if (temp == 0x8103) //data read as little endian order (actual data order for Bit String is 03 81)
					reader.ReadByte(); //advance 1 byte
				else if (temp == 0x8203)
					reader.ReadInt16(); //advance 2 bytes
				else
					throw new FormatException("Expected 0x8130 or 0x8230 but read {0:X4}".FormatInvariant(temp));

				var bt = reader.ReadByte();
				if (bt != 0x00) //expect null byte next
					throw new FormatException("Expected 0x00 but read {0:X2}".FormatInvariant(bt));

				temp = reader.ReadUInt16();
				if (temp == 0x8130) //data read as little endian order (actual data order for Sequence is 30 81)
					reader.ReadByte(); //advance 1 byte
				else if (temp == 0x8230)
					reader.ReadInt16(); //advance 2 bytes
				else
					throw new FormatException("Expected 0x8130 or 0x8230 but read {0:X4}".FormatInvariant(temp));

				temp = reader.ReadUInt16();
				byte lowbyte;
				byte highbyte = 0x00;

				if (temp == 0x8102)
				{
					//data read as little endian order (actual data order for Integer is 02 81)
					lowbyte = reader.ReadByte(); // read next bytes which is bytes in modulus
				}
				else if (temp == 0x8202)
				{
					highbyte = reader.ReadByte(); //advance 2 bytes
					lowbyte = reader.ReadByte();
				}
				else
				{
					throw new FormatException("Expected 0x8102 or 0x8202 but read {0:X4}".FormatInvariant(temp));
				}

				var modulusSize = highbyte * 256 + lowbyte;

				var firstbyte = reader.ReadByte();
				reader.BaseStream.Seek(-1, SeekOrigin.Current);

				if (firstbyte == 0x00)
				{
					//if first byte (highest order) of modulus is zero, don't include it
					reader.ReadByte(); //skip this null byte
					modulusSize -= 1; //reduce modulus buffer size by 1
				}

				var modulus = reader.ReadBytes(modulusSize); //read the modulus bytes

				if (reader.ReadByte() != 0x02) //expect an Integer for the exponent data
					throw new FormatException("Expected 0x02");
				int exponentSize = reader.ReadByte(); // should only need one byte for actual exponent data (for all useful values)
				var exponent = reader.ReadBytes(exponentSize);

				// ------- create RSACryptoServiceProvider instance and initialize with public key -----
				var rsa = RSA.Create();
				var rsaKeyInfo = new RSAParameters
				{
					Modulus = modulus,
					Exponent = exponent
				};
				rsa.ImportParameters(rsaKeyInfo);
				return rsa;
			}
		}

		/// <summary>
		/// Returns a new <see cref="ArraySegment{T}"/> that starts at index <paramref name="index"/> into <paramref name="arraySegment"/>.
		/// </summary>
		/// <param name="arraySegment">The <see cref="ArraySegment{T}"/> from which to create a slice.</param>
		/// <param name="index">The non-negative, zero-based starting index of the new slice (relative to <see cref="ArraySegment{T}.Offset"/> of <paramref name="arraySegment"/>.</param>
		/// <returns>A new <see cref="ArraySegment{T}"/> starting at the <paramref name="index"/>th element of <paramref name="arraySegment"/> and continuing to the end of <paramref name="arraySegment"/>.</returns>
		public static ArraySegment<T> Slice<T>(this ArraySegment<T> arraySegment, int index) =>
			new ArraySegment<T>(arraySegment.Array, arraySegment.Offset + index, arraySegment.Count - index);

		/// <summary>
		/// Returns a new <see cref="ArraySegment{T}"/> that starts at index <paramref name="index"/> into <paramref name="arraySegment"/> and has a length of <paramref name="length"/>.
		/// </summary>
		/// <param name="arraySegment">The <see cref="ArraySegment{T}"/> from which to create a slice.</param>
		/// <param name="index">The non-negative, zero-based starting index of the new slice (relative to <see cref="ArraySegment{T}.Offset"/> of <paramref name="arraySegment"/>.</param>
		/// <param name="length">The non-negative length of the new slice.</param>
		/// <returns>A new <see cref="ArraySegment{T}"/> of length <paramref name="length"/>, starting at the <paramref name="index"/>th element of <paramref name="arraySegment"/>.</returns>
		public static ArraySegment<T> Slice<T>(this ArraySegment<T> arraySegment, int index, int length) =>
			new ArraySegment<T>(arraySegment.Array, arraySegment.Offset + index, length);

#if NET45
		public static Task<T> TaskFromException<T>(Exception exception)
		{
			var tcs = new TaskCompletionSource<T>();
			tcs.SetException(exception);
			return tcs.Task;
		}
#else
		public static Task<T> TaskFromException<T>(Exception exception) => Task.FromException<T>(exception);
#endif

		public static byte[] TrimZeroByte(byte[] value)
		{
			if (value[value.Length - 1] == 0)
				Array.Resize(ref value, value.Length - 1);
			return value;
		}

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

		public static void WriteUtf8(this BinaryWriter writer, string value) =>
			WriteUtf8(writer, value, 0, value.Length);

		public static void WriteUtf8(this BinaryWriter writer, string value, int startIndex, int length)
		{
			var endIndex = startIndex + length;
			while (startIndex < endIndex)
			{
				int codePoint = char.ConvertToUtf32(value, startIndex);
				startIndex++;
				if (codePoint < 0x80)
				{
					writer.Write((byte) codePoint);
				}
				else if (codePoint < 0x800)
				{
					writer.Write((byte) (0xC0 | ((codePoint >> 6) & 0x1F)));
					writer.Write((byte) (0x80 | (codePoint & 0x3F)));
				}
				else if (codePoint < 0x10000)
				{
					writer.Write((byte) (0xE0 | ((codePoint >> 12) & 0x0F)));
					writer.Write((byte) (0x80 | ((codePoint >> 6) & 0x3F)));
					writer.Write((byte) (0x80 | (codePoint & 0x3F)));
				}
				else
				{
					writer.Write((byte) (0xF0 | ((codePoint >> 18) & 0x07)));
					writer.Write((byte) (0x80 | ((codePoint >> 12) & 0x3F)));
					writer.Write((byte) (0x80 | ((codePoint >> 6) & 0x3F)));
					writer.Write((byte) (0x80 | (codePoint & 0x3F)));
					startIndex++;
				}
			}
		}

#if NET45 || NET46
		public static bool IsWindows() => Environment.OSVersion.Platform == PlatformID.Win32NT;

		public static void GetOSDetails(out string os, out string osDescription, out string architecture)
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

		public static void GetOSDetails(out string os, out string osDescription, out string architecture)
		{
			os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" :
				RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux" :
				RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS" : null;
			osDescription = RuntimeInformation.OSDescription;
			architecture = RuntimeInformation.ProcessArchitecture.ToString();
		}
#endif

#if NET45 || NET46
		public static SslProtocols GetDefaultSslProtocols()
		{
			if (!s_defaultSslProtocols.HasValue)
			{
				// Prior to .NET Framework 4.7, SslProtocols.None is not a valid argument to SslStream.AuthenticateAsClientAsync.
				// If the NET46 build is loaded by an application that targets. NET 4.7 (or later), or if app.config has set
				// Switch.System.Net.DontEnableSystemDefaultTlsVersions to false, then SslProtocols.None will work; otherwise,
				// if the application targets .NET 4.6.2 or earlier and hasn't changed the AppContext switch, then it will
				// fail at runtime. We attempt to determine if it will fail by accessing the internal static
				// ServicePointManager.DisableSystemDefaultTlsVersions property, which controls whether SslProtocols.None is
				// an acceptable value.
				bool disableSystemDefaultTlsVersions;
				try
				{
					var property = typeof(ServicePointManager).GetProperty("DisableSystemDefaultTlsVersions", BindingFlags.NonPublic | BindingFlags.Static);
					disableSystemDefaultTlsVersions = property == null || (property.GetValue(null) is bool b && b);
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
	}
}
