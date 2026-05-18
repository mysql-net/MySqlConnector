#if NET6_0_OR_GREATER

using System.Buffers;
using System.Text;
using System.Threading.Channels;
using MySqlConnector.Helpers;
using MySqlConnector.Protocol;
using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector;

public sealed class MySqlBulkImport : IAsyncDisposable
{
	private readonly int bufferSize;
	private readonly MySqlConnection m_connection;
	private readonly ArrayPool<byte> pool;
	private Channel<BufferData> channel = Channel.CreateUnbounded<BufferData>(new UnboundedChannelOptions
	{
		SingleReader = true,
		SingleWriter = true,
		AllowSynchronousContinuations = false,
	});

	private Encoder? encoder;
	private BufferData? bufferData;
	private int columnsCount;
	private Task? importTask;

	/// <summary>
	/// Initializes a <see cref="MySqlBulkCopy"/> object with the specified connection, and optionally the active transaction.
	/// </summary>
	/// <param name="connection">The <see cref="MySqlConnection"/> to use.</param>
	/// <param name="bufferSize">The size of the buffers requested from the pool</param>
	/// <param name="pool">Pool for obtaining temporary buffers for writing data</param>
	public MySqlBulkImport(
		MySqlConnection connection,
		int bufferSize = 65_536,
		ArrayPool<byte>? pool = null)
	{
		ArgumentNullException.ThrowIfNull(connection);
		if (bufferSize < 1_000)
		{
			throw new ArgumentException("The buffer size cannot be less than 1000 bytes.", nameof(bufferSize));
		}

		this.bufferSize = bufferSize;
		m_connection = connection;
		this.pool = pool ?? ArrayPool<byte>.Shared;
	}

	/// <summary>
	/// A <see cref="MySqlBulkLoaderConflictOption"/> value that specifies how conflicts are resolved (default <see cref="MySqlBulkLoaderConflictOption.None"/>).
	/// </summary>
	public MySqlBulkLoaderConflictOption ConflictOption { get; set; }

	public void StartImport(
		string destinationTableName,
		string[] columns,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(destinationTableName);
		ArgumentNullException.ThrowIfNull(columns);
		if (columns.Length == 0)
		{
			throw new ArgumentException("The column array is empty", nameof(columns));
		}

		if (importTask != null)
		{
			throw new InvalidOperationException($"First you need to wait for the previous import to complete by calling '{nameof(WaitFinishImportAsync)}'");
		}

		cancellationToken.ThrowIfCancellationRequested();
		var tableName = destinationTableName ?? throw new InvalidOperationException("DestinationTableName must be set before calling WriteToServer");
		var bulkLoader = new MySqlBulkLoader(m_connection)
		{
			CharacterSet = "utf8mb4",
			EscapeCharacter = '\\',
			FieldQuotationCharacter = '\0',
			FieldTerminator = "\t",
			LinePrefix = null,
			LineTerminator = "\n",
			Local = true,
			NumberOfLinesToSkip = 0,
			Source = this,
			TableName = tableName,
			Timeout = 0,
			ConflictOption = ConflictOption,
		};

		bulkLoader.Columns.AddRange(columns);

		var closeConnection = false;
		if (m_connection.State != ConnectionState.Open)
		{
			m_connection.Open();
			closeConnection = true;
		}

		importTask = Task.Run(async () =>
		{
			try
			{
				var errors = new List<MySqlError>();
				MySqlInfoMessageEventHandler infoMessageHandler = (s, e) => errors.AddRange(e.Errors);
				m_connection.InfoMessage += infoMessageHandler;

				int rowsInserted;
				try
				{
					rowsInserted = await bulkLoader.LoadAsync(IOBehavior.Asynchronous, cancellationToken).ConfigureAwait(false);
				}
				finally
				{
					m_connection.InfoMessage -= infoMessageHandler;
				}
			}
			finally
			{
				if (closeConnection)
					await m_connection.CloseAsync().ConfigureAwait(false);
			}
		},
		cancellationToken);
	}

	public async Task WaitFinishImportAsync()
	{
		if (importTask == null)
		{
			return;
		}
		else
		{
			if (columnsCount != 0)
			{
				throw new InvalidOperationException($"Before calling '{nameof(WaitFinishImportAsync)}', you must close the line write by calling '{nameof(EndRow)}'");
			}

			var writer = channel.Writer;
			if (bufferData != null)
			{
				writer.TryWrite(bufferData);
				bufferData = null;
			}

			writer.Complete();
			columnsCount = 0;
			encoder = null;

			try
			{
				await importTask.ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				// ignore
			}

			importTask = null;

			channel = Channel.CreateUnbounded<BufferData>(new UnboundedChannelOptions
			{
				SingleReader = true,
				SingleWriter = true,
				AllowSynchronousContinuations = false,
			});
		}
	}

	public void WriteColumnValue<T>(T value)
	{
		bufferData ??= new BufferData(pool.Rent(bufferSize), 0);

		var totalBytesWritten = bufferData.TotalBytesWritten;
		var span = bufferData.Buffer.AsSpan();
		var inputIndex = 0;

		if (totalBytesWritten >= bufferSize)
		{
			channel.Writer.TryWrite(bufferData);
			bufferData = new BufferData(pool.Rent(bufferSize), 0);
			totalBytesWritten = 0;
			span = bufferData.Buffer.AsSpan();
		}

		if (columnsCount++ > 0)
		{
			const byte tabByte = (byte) '\t';
			span[..bufferSize][totalBytesWritten++] = tabByte;
		}

		while (true)
		{
			var completeWrite = ValueWriteHelper.WriteValue(
				m_connection,
				value,
				ref inputIndex,
				ref encoder,
				span[..bufferSize][totalBytesWritten..],
				out var bytesWritten);

			totalBytesWritten += bytesWritten;
			bufferData.TotalBytesWritten = totalBytesWritten;
			if (!completeWrite)
			{
				channel.Writer.TryWrite(bufferData);
				bufferData = new BufferData(pool.Rent(bufferSize), 0);
				totalBytesWritten = 0;
				span = bufferData.Buffer.AsSpan();
			}
			else
			{
				break;
			}
		}
	}

	public void EndRow()
	{
		if (columnsCount <= 0)
		{
			throw new InvalidOperationException("The row does not contain any values.");
		}

		var totalBytesWritten = bufferData!.TotalBytesWritten;
		var span = bufferData.Buffer.AsSpan();

		if (totalBytesWritten >= bufferSize)
		{
			channel.Writer.TryWrite(bufferData);
			bufferData = new BufferData(pool.Rent(bufferSize), 0);
			totalBytesWritten = 0;
			span = bufferData.Buffer.AsSpan();
		}

		const byte newLineByte = (byte) '\n';
		span[..bufferSize][totalBytesWritten++] = newLineByte;
		bufferData!.TotalBytesWritten = totalBytesWritten;
		columnsCount = 0;
	}

	internal async Task SendDataReaderAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		var reader = channel.Reader;
		while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
		{
			while (reader.TryRead(out var bufferData))
			{
				try
				{
					var payload = new PayloadData(new ArraySegment<byte>(bufferData.Buffer, 0, bufferData.TotalBytesWritten));
					await m_connection.Session.SendReplyAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
				}
				finally
				{
					pool.Return(bufferData.Buffer);
				}
			}
		}
	}

	public async ValueTask DisposeAsync()
	{
		channel.Writer.TryComplete();
		if (bufferData != null)
		{
			try
			{
				pool.Return(bufferData.Buffer);
			}
			catch
			{
				// ignore
			}

			bufferData = null;
		}

		if (importTask != null)
		{
			try
			{
				await importTask.ConfigureAwait(false);
				importTask = null;
			}
			catch
			{
				// ignore
			}
		}

		while (channel.Reader.TryRead(out var bufferData))
		{
			try
			{
				pool.Return(bufferData.Buffer);
			}
			catch
			{
				// ignore
			}
		}
	}

	private sealed class BufferData
	{
		public BufferData(
			byte[] buffer,
			int totalBytesWritten)
		{
			Buffer = buffer;
			TotalBytesWritten = totalBytesWritten;
		}

		public int TotalBytesWritten { get; set; }

		public byte[] Buffer { get; private set; }
	}
}

public sealed class MySqlBulkImport2 : IAsyncDisposable
{
	private readonly int bufferSize;
	private readonly MySqlConnection m_connection;
	private readonly ArrayPool<byte> pool;
	private Channel<BufferData> channel = Channel.CreateUnbounded<BufferData>(new UnboundedChannelOptions
	{
		SingleReader = true,
		SingleWriter = true,
		AllowSynchronousContinuations = false,
	});

	private Encoder? encoder;
	private BufferData? bufferData;
	private int columnsCount;
	private Task? importTask;

	/// <summary>
	/// Initializes a <see cref="MySqlBulkCopy"/> object with the specified connection, and optionally the active transaction.
	/// </summary>
	/// <param name="connection">The <see cref="MySqlConnection"/> to use.</param>
	/// <param name="bufferSize">The size of the buffers requested from the pool</param>
	/// <param name="pool">Pool for obtaining temporary buffers for writing data</param>
	public MySqlBulkImport2(
		MySqlConnection connection,
		int bufferSize = 65_536,
		ArrayPool<byte>? pool = null)
	{
		ArgumentNullException.ThrowIfNull(connection);
		if (bufferSize < 1_000)
		{
			throw new ArgumentException("The buffer size cannot be less than 1000 bytes.", nameof(bufferSize));
		}

		this.bufferSize = bufferSize;
		m_connection = connection;
		this.pool = pool ?? ArrayPool<byte>.Shared;
	}

	/// <summary>
	/// A <see cref="MySqlBulkLoaderConflictOption"/> value that specifies how conflicts are resolved (default <see cref="MySqlBulkLoaderConflictOption.None"/>).
	/// </summary>
	public MySqlBulkLoaderConflictOption ConflictOption { get; set; }

	public void StartImport(
		string destinationTableName,
		string[] columns,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(destinationTableName);
		ArgumentNullException.ThrowIfNull(columns);
		if (columns.Length == 0)
		{
			throw new ArgumentException("The column array is empty", nameof(columns));
		}

		if (importTask != null)
		{
			throw new InvalidOperationException($"First you need to wait for the previous import to complete by calling '{nameof(WaitFinishImportAsync)}'");
		}

		cancellationToken.ThrowIfCancellationRequested();
		var tableName = destinationTableName ?? throw new InvalidOperationException("DestinationTableName must be set before calling WriteToServer");
		var bulkLoader = new MySqlBulkLoader(m_connection)
		{
			CharacterSet = "utf8mb4",
			EscapeCharacter = '\\',
			FieldQuotationCharacter = '\0',
			FieldTerminator = "\t",
			LinePrefix = null,
			LineTerminator = "\n",
			Local = true,
			NumberOfLinesToSkip = 0,
			Source = this,
			TableName = tableName,
			Timeout = 0,
			ConflictOption = ConflictOption,
		};

		bulkLoader.Columns.AddRange(columns);

		var closeConnection = false;
		if (m_connection.State != ConnectionState.Open)
		{
			m_connection.Open();
			closeConnection = true;
		}

		importTask = Task.Run(async () =>
		{
			try
			{
				var errors = new List<MySqlError>();
				MySqlInfoMessageEventHandler infoMessageHandler = (s, e) => errors.AddRange(e.Errors);
				m_connection.InfoMessage += infoMessageHandler;

				int rowsInserted;
				try
				{
					rowsInserted = await bulkLoader.LoadAsync(IOBehavior.Asynchronous, cancellationToken).ConfigureAwait(false);
				}
				finally
				{
					m_connection.InfoMessage -= infoMessageHandler;
				}
			}
			finally
			{
				if (closeConnection)
					await m_connection.CloseAsync().ConfigureAwait(false);
			}
		},
		cancellationToken);
	}

	public async Task WaitFinishImportAsync()
	{
		if (importTask == null)
		{
			return;
		}
		else
		{
			if (columnsCount != 0)
			{
				throw new InvalidOperationException($"Before calling '{nameof(WaitFinishImportAsync)}', you must close the line write by calling '{nameof(EndRow)}'");
			}

			var writer = channel.Writer;
			if (bufferData != null)
			{
				writer.TryWrite(bufferData);
				bufferData = null;
			}

			writer.Complete();
			columnsCount = 0;
			encoder = null;

			try
			{
				await importTask.ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				// ignore
			}

			importTask = null;

			channel = Channel.CreateUnbounded<BufferData>(new UnboundedChannelOptions
			{
				SingleReader = true,
				SingleWriter = true,
				AllowSynchronousContinuations = false,
			});
		}
	}

	public void WriteColumnValue<T>(T value)
	{
		bufferData ??= new BufferData(pool.Rent(bufferSize), 0);

		var totalBytesWritten = bufferData.TotalBytesWritten;
		var span = bufferData.Buffer.AsSpan();
		var inputIndex = 0;

		if (totalBytesWritten >= bufferSize)
		{
			channel.Writer.TryWrite(bufferData);
			bufferData = new BufferData(pool.Rent(bufferSize), 0);
			totalBytesWritten = 0;
			span = bufferData.Buffer.AsSpan();
		}

		if (columnsCount++ > 0)
		{
			const byte tabByte = (byte) '\t';
			span[..bufferSize][totalBytesWritten++] = tabByte;
		}

		while (true)
		{
			var completeWrite = ValueWriteHelper.WriteValue(
				m_connection,
				value,
				ref inputIndex,
				ref encoder,
				span[..bufferSize][totalBytesWritten..],
				out var bytesWritten);

			totalBytesWritten += bytesWritten;
			bufferData.TotalBytesWritten = totalBytesWritten;
			if (!completeWrite)
			{
				channel.Writer.TryWrite(bufferData);
				bufferData = new BufferData(pool.Rent(bufferSize), 0);
				totalBytesWritten = 0;
				span = bufferData.Buffer.AsSpan();
			}
			else
			{
				break;
			}
		}
	}

	public void EndRow()
	{
		if (columnsCount <= 0)
		{
			throw new InvalidOperationException("The row does not contain any values.");
		}

		var totalBytesWritten = bufferData!.TotalBytesWritten;
		var span = bufferData.Buffer.AsSpan();

		if (totalBytesWritten >= bufferSize)
		{
			channel.Writer.TryWrite(bufferData);
			bufferData = new BufferData(pool.Rent(bufferSize), 0);
			totalBytesWritten = 0;
			span = bufferData.Buffer.AsSpan();
		}

		const byte newLineByte = (byte) '\n';
		span[..bufferSize][totalBytesWritten++] = newLineByte;
		bufferData!.TotalBytesWritten = totalBytesWritten;
		columnsCount = 0;
	}

	internal async Task SendDataReaderAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		var reader = channel.Reader;
		while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
		{
			while (reader.TryRead(out var bufferData))
			{
				try
				{
					var payload = new PayloadData(new ArraySegment<byte>(bufferData.Buffer, 0, bufferData.TotalBytesWritten));
					await m_connection.Session.SendReplyAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
				}
				finally
				{
					pool.Return(bufferData.Buffer);
				}
			}
		}
	}

	public async ValueTask DisposeAsync()
	{
		channel.Writer.TryComplete();
		if (bufferData != null)
		{
			try
			{
				pool.Return(bufferData.Buffer);
			}
			catch
			{
				// ignore
			}

			bufferData = null;
		}

		if (importTask != null)
		{
			try
			{
				await importTask.ConfigureAwait(false);
				importTask = null;
			}
			catch
			{
				// ignore
			}
		}

		while (channel.Reader.TryRead(out var bufferData))
		{
			try
			{
				pool.Return(bufferData.Buffer);
			}
			catch
			{
				// ignore
			}
		}
	}

	private sealed class BufferData
	{
		public BufferData(
			byte[] buffer,
			int totalBytesWritten)
		{
			Buffer = buffer;
			TotalBytesWritten = totalBytesWritten;
		}

		public int TotalBytesWritten { get; set; }

		public byte[] Buffer { get; private set; }
	}
}

#endif
