using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Diagnostics;
using System.Text;
using MySqlConnector.Utilities;

namespace MySqlConnector.Protocol.Serialization;

internal sealed class ByteBufferWriter : IBufferWriter<byte>
{
	public ByteBufferWriter(int capacity = 0)
	{
		m_buffer = ArrayPool<byte>.Shared.Rent(Math.Max(capacity, 128));
		m_output = m_buffer;
	}

	public int Position => m_buffer.Length - m_output.Length;

	public ArraySegment<byte> ArraySegment => new ArraySegment<byte>(m_buffer, 0, Position);

	public PayloadData ToPayloadData() => new PayloadData(ArraySegment, isPooled: true);

	public Memory<byte> GetMemory(int sizeHint = 0)
	{
		Debug.Assert(sizeHint >= 0, "sizeHint >= 0");
		if (sizeHint > m_output.Length)
			Reallocate(sizeHint);
		return m_output;
	}

	public Span<byte> GetSpan(int sizeHint = 0)
	{
		Debug.Assert(sizeHint >= 0, "sizeHint >= 0");
		if (sizeHint > m_output.Length)
			Reallocate(sizeHint);
		return m_output.Span;
	}

	public void Advance(int count)
	{
		Debug.Assert(count <= m_output.Length, "count <= m_output.Length");
		m_output = m_output.Slice(count);
	}

	public void TrimEnd(int byteCount)
	{
		Debug.Assert(byteCount <= m_output.Length, "byteCount <= m_output.Length");
		m_output = m_buffer.AsMemory().Slice(Position - byteCount);
	}

	public void Write(byte value)
	{
		if (m_output.Length < 1)
			Reallocate();
		m_output.Span[0] = value;
		m_output = m_output.Slice(1);
	}

	public void Write(ushort value)
	{
		if (m_output.Length < 2)
			Reallocate(2);
		BinaryPrimitives.WriteUInt16LittleEndian(m_output.Span, value);
		m_output = m_output.Slice(2);
	}

	public void Write(int value)
	{
		if (m_output.Length < 4)
			Reallocate(4);
		BinaryPrimitives.WriteInt32LittleEndian(m_output.Span, value);
		m_output = m_output.Slice(4);
	}

	public void Write(uint value)
	{
		if (m_output.Length < 4)
			Reallocate(4);
		BinaryPrimitives.WriteUInt32LittleEndian(m_output.Span, value);
		m_output = m_output.Slice(4);
	}

	public void Write(ulong value)
	{
		if (m_output.Length < 8)
			Reallocate(8);
		BinaryPrimitives.WriteUInt64LittleEndian(m_output.Span, value);
		m_output = m_output.Slice(8);
	}

	public void Write(ArraySegment<byte> arraySegment) => Write(arraySegment.AsSpan());

	public void Write(ReadOnlySpan<byte> span)
	{
		if (m_output.Length < span.Length)
			Reallocate(span.Length);
		span.CopyTo(m_output.Span);
		m_output = m_output.Slice(span.Length);
	}

#if NET45
	public void Write(string value)
	{
		Debug.Assert(value is not null, "value is not null");
		if (value!.Length == 0)
			return;

		var byteCount = Encoding.UTF8.GetByteCount(value);
		if (byteCount > m_output.Length)
			Reallocate(byteCount);
		Encoding.UTF8.GetBytes(value.AsSpan(), m_output.Span);
		m_output = m_output.Slice(byteCount);
	}

	public unsafe void Write(string value, int offset, int length)
	{
		if (length == 0)
			return;

		Debug.Assert(value is not null, "value is not null");
		fixed (char* charsPtr = value)
		{
			var byteCount = Encoding.UTF8.GetByteCount(charsPtr + offset, length);
			if (byteCount > m_output.Length)
				Reallocate(byteCount);
			Encoding.UTF8.GetBytes(value.AsSpan(offset, length), m_output.Span);
			m_output = m_output.Slice(byteCount);
		}
	}
#else
	public void Write(string value) => Write(value.AsSpan(), flush: true);
	public void Write(string value, int offset, int length) => Write(value.AsSpan(offset, length), flush: true);

	public void Write(ReadOnlySpan<char> chars, bool flush)
	{
		m_encoder ??= Encoding.UTF8.GetEncoder();
		while (chars.Length > 0)
		{
			if (m_output.Length < 4)
				Reallocate();
			m_encoder.Convert(chars, m_output.Span, flush: false, out var charsUsed, out var bytesUsed, out var completed);
			chars = chars.Slice(charsUsed);
			m_output = m_output.Slice(bytesUsed);
			if (!completed)
				Reallocate();
			Debug.Assert(completed == (chars.Length == 0));
		}

		if (flush && m_encoder is not null)
		{
			if (m_output.Length < 4)
				Reallocate();
			m_encoder.Convert("".AsSpan(), m_output.Span, flush: true, out _, out var bytesUsed, out _);
			m_output = m_output.Slice(bytesUsed);
		}
	}
#endif

	public void WriteLengthEncodedString(StringBuilder stringBuilder)
	{
#if NETCOREAPP3_0_OR_GREATER
		// save where the length will be written
		var lengthPosition = Position;
		if (m_output.Length < 9)
			Reallocate(9);
		Advance(9);

		// write all the text as UTF-8
		m_encoder ??= Encoding.UTF8.GetEncoder();
		foreach (var chunk in stringBuilder.GetChunks())
		{
			var currentSpan = chunk.Span;
			while (currentSpan.Length > 0)
			{
				if (m_output.Length < 4)
					Reallocate();
				m_encoder.Convert(currentSpan, m_output.Span, false, out var charsUsed, out var bytesUsed, out var completed);
				currentSpan = currentSpan.Slice(charsUsed);
				m_output = m_output.Slice(bytesUsed);
				if (!completed)
					Reallocate();
				Debug.Assert(completed == (currentSpan.Length == 0));
			}
		}

		// flush the output
		if (m_output.Length < 4)
			Reallocate();
		m_encoder.Convert("".AsSpan(), m_output.Span, true, out _, out var finalBytesUsed, out _);
		m_output = m_output.Slice(finalBytesUsed);

		// write the length (as a 64-bit integer) in the reserved space
		var textLength = Position - (lengthPosition + 9);
		m_buffer[lengthPosition] = 0xFE;
		BinaryPrimitives.WriteUInt64LittleEndian(m_buffer.AsSpan(lengthPosition + 1), (ulong) textLength);
#else
		this.WriteLengthEncodedString(stringBuilder.ToString());
#endif
	}

	public void WriteString(short value)
	{
		int bytesWritten;
		while (!Utf8Formatter.TryFormat(value, m_output.Span, out bytesWritten))
			Reallocate();
		m_output = m_output.Slice(bytesWritten);
	}

	public void WriteString(ushort value)
	{
		int bytesWritten;
		while (!Utf8Formatter.TryFormat(value, m_output.Span, out bytesWritten))
			Reallocate();
		m_output = m_output.Slice(bytesWritten);
	}

	public void WriteString(int value)
	{
		int bytesWritten;
		while (!Utf8Formatter.TryFormat(value, m_output.Span, out bytesWritten))
			Reallocate();
		m_output = m_output.Slice(bytesWritten);
	}

	public void WriteString(uint value)
	{
		int bytesWritten;
		while (!Utf8Formatter.TryFormat(value, m_output.Span, out bytesWritten))
			Reallocate();
		m_output = m_output.Slice(bytesWritten);
	}

	public void WriteString(long value)
	{
		int bytesWritten;
		while (!Utf8Formatter.TryFormat(value, m_output.Span, out bytesWritten))
			Reallocate();
		m_output = m_output.Slice(bytesWritten);
	}

	public void WriteString(ulong value)
	{
		int bytesWritten;
		while (!Utf8Formatter.TryFormat(value, m_output.Span, out bytesWritten))
			Reallocate();
		m_output = m_output.Slice(bytesWritten);
	}

	private void Reallocate(int additional = 0)
	{
		var usedLength = Position;
		var newBuffer = ArrayPool<byte>.Shared.Rent(Math.Max(usedLength + additional, m_buffer.Length * 2));
		Buffer.BlockCopy(m_buffer, 0, newBuffer, 0, usedLength);
		ArrayPool<byte>.Shared.Return(m_buffer);
		m_buffer = newBuffer;
		m_output = new(m_buffer, usedLength, m_buffer.Length - usedLength);
	}

#if !NET45
	Encoder? m_encoder;
#endif
	byte[] m_buffer;
	Memory<byte> m_output;
}

internal static class ByteBufferWriterExtensions
{
	public static void WriteLengthEncodedInteger(this ByteBufferWriter writer, ulong value)
	{
		switch (value)
		{
		case < 251:
			writer.Write((byte) value);
			break;

		case < 65536:
			writer.Write((byte) 0xfc);
			writer.Write((ushort) value);
			break;

		case < 16777216:
			writer.Write((uint) ((value << 8) | 0xfd));
			break;

		default:
			writer.Write((byte) 0xfe);
			writer.Write(value);
			break;
		}
	}

#if NET45
	public static void WriteLengthEncodedString(this ByteBufferWriter writer, string value)
	{
		var byteCount = Encoding.UTF8.GetByteCount(value);
		writer.WriteLengthEncodedInteger((ulong) byteCount);
		writer.Write(value);
	}
#else
	public static void WriteLengthEncodedString(this ByteBufferWriter writer, string value) => writer.WriteLengthEncodedString(value.AsSpan());

	public static void WriteLengthEncodedString(this ByteBufferWriter writer, ReadOnlySpan<char> value)
	{
		var byteCount = Encoding.UTF8.GetByteCount(value);
		writer.WriteLengthEncodedInteger((ulong) byteCount);
		writer.Write(value, flush: true);
	}
#endif

	public static void WriteNullTerminatedString(this ByteBufferWriter writer, string value)
	{
		writer.Write(value);
		writer.Write((byte) 0);
	}
}
