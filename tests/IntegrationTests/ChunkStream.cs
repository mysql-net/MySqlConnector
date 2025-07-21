namespace IntegrationTests;

internal sealed class ChunkStream : Stream
{
	public ChunkStream(byte[] data, int chunkLength)
	{
		if (data is null)
			throw new ArgumentNullException(nameof(data));
		if (chunkLength <= 0)
			throw new ArgumentOutOfRangeException(nameof(chunkLength));

		m_data = data;
		m_chunkLength = chunkLength;
		m_position = 0;
	}

	public override bool CanRead => true;
	public override bool CanSeek => false;
	public override bool CanWrite => false;
	public override long Length => m_data.Length;
	public override long Position
	{
		get => m_position;
		set => throw new NotSupportedException();
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		if (buffer is null)
			throw new ArgumentNullException(nameof(buffer));
		if (offset < 0 || offset > buffer.Length)
			throw new ArgumentOutOfRangeException(nameof(offset));
		if (count < 0 || offset + count > buffer.Length)
			throw new ArgumentOutOfRangeException(nameof(count));

		return Read(buffer.AsSpan(offset, count));
	}

	public
#if NETSTANDARD2_1 || NETCOREAPP2_1_OR_GREATER
		override
#endif
		int Read(Span<byte> buffer)
	{
		if (m_position >= m_data.Length)
			return 0;

		// Read at most chunkLength bytes
		var bytesToRead = Math.Min(buffer.Length, Math.Min(m_chunkLength, m_data.Length - m_position));

		// Copy data from the actual data array
		m_data.AsSpan(m_position, bytesToRead).CopyTo(buffer);

		m_position += bytesToRead;
		return bytesToRead;
	}

	public override int ReadByte()
	{
		Span<byte> buffer = stackalloc byte[1];
		var bytesRead = Read(buffer);
		return bytesRead == 0 ? -1 : buffer[0];
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		if (buffer is null)
			throw new ArgumentNullException(nameof(buffer));
		if (offset < 0 || offset > buffer.Length)
			throw new ArgumentOutOfRangeException(nameof(offset));
		if (count < 0 || offset + count > buffer.Length)
			throw new ArgumentOutOfRangeException(nameof(count));

		if (cancellationToken.IsCancellationRequested)
			return Task.FromCanceled<int>(cancellationToken);

		try
		{
			return Task.FromResult(Read(buffer.AsSpan(offset, count)));
		}
		catch (Exception ex)
		{
			return Task.FromException<int>(ex);
		}
	}

	public
#if NETSTANDARD2_1 || NETCOREAPP2_1_OR_GREATER
		override
#endif
		ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return new(Task.FromCanceled<int>(cancellationToken));

		try
		{
			return new(Read(buffer.Span));
		}
		catch (Exception ex)
		{
			return new(Task.FromException<int>(ex));
		}
	}

	public override void Write(byte[] buffer, int offset, int count) =>
		throw new NotSupportedException();

	public override void WriteByte(byte value) =>
		throw new NotSupportedException();

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
		throw new NotSupportedException();

	public override void SetLength(long value) =>
		throw new NotSupportedException();

	public override long Seek(long offset, SeekOrigin origin) =>
		throw new NotSupportedException();

	public override void Flush() =>
		throw new NotSupportedException();

	public override Task FlushAsync(CancellationToken cancellationToken) =>
		throw new NotSupportedException();

	private readonly byte[] m_data;
	private readonly int m_chunkLength;
	private int m_position;
}
