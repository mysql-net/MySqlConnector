using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.ExceptionServices;
using MySqlConnector.Protocol;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core;

internal sealed class ResultSet
{
	public ResultSet(MySqlDataReader dataReader)
	{
		DataReader = dataReader;
	}

	public void Reset()
	{
		// ResultSet can be re-used, so initialize everything
		BufferState = ResultSetState.None;
		ColumnDefinitions = null;
		ColumnTypes = null;
		RecordsAffected = null;
		WarningCount = 0;
		State = ResultSetState.None;
		ContainsCommandParameters = false;
		m_columnDefinitionPayloadUsedBytes = 0;
		m_readBuffer?.Clear();
		m_row = null;
		m_hasRows = false;
		ReadResultSetHeaderException = null;
	}

	public async Task ReadResultSetHeaderAsync(IOBehavior ioBehavior)
	{
		Reset();

		try
		{
			while (true)
			{
				var payload = await Session.ReceiveReplyAsync(ioBehavior, CancellationToken.None).ConfigureAwait(false);
				var firstByte = payload.HeaderByte;
				if (firstByte == OkPayload.Signature)
				{
					var ok = OkPayload.Create(payload.Span, Session.SupportsDeprecateEof, Session.SupportsSessionTrack);
					RecordsAffected = (RecordsAffected ?? 0) + ok.AffectedRowCount;
					if (ok.LastInsertId != 0)
						Command?.SetLastInsertedId((long) ok.LastInsertId);
					WarningCount = ok.WarningCount;
					if (ok.NewSchema is not null)
						Connection.Session.DatabaseOverride = ok.NewSchema;
					ColumnDefinitions = null;
					ColumnTypes = null;
					State = (ok.ServerStatus & ServerStatus.MoreResultsExist) == 0
						? ResultSetState.NoMoreData
						: ResultSetState.HasMoreData;
					if (State == ResultSetState.NoMoreData)
						break;
				}
				else if (firstByte == LocalInfilePayload.Signature)
				{
					try
					{
						if (!Connection.AllowLoadLocalInfile)
							throw new NotSupportedException("To use LOAD DATA LOCAL INFILE, set AllowLoadLocalInfile=true in the connection string. See https://fl.vu/mysql-load-data");
						var localInfile = LocalInfilePayload.Create(payload.Span);
						var hasSourcePrefix = localInfile.FileName.StartsWith(MySqlBulkLoader.SourcePrefix, StringComparison.Ordinal);
						if (!IsHostVerified(Connection) && !hasSourcePrefix)
							throw new NotSupportedException("Use SourceStream or SslMode >= VerifyCA for LOAD DATA LOCAL INFILE. See https://fl.vu/mysql-load-data");

						var source = hasSourcePrefix ?
							MySqlBulkLoader.GetAndRemoveSource(localInfile.FileName) :
							File.OpenRead(localInfile.FileName);
						switch (source)
						{
						case Stream stream:
							var buffer = ArrayPool<byte>.Shared.Rent(1048576);
							try
							{
								int byteCount;
								while ((byteCount = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
								{
									payload = new(new ArraySegment<byte>(buffer, 0, byteCount));
									await Session.SendReplyAsync(payload, ioBehavior, CancellationToken.None).ConfigureAwait(false);
								}
							}
							finally
							{
								ArrayPool<byte>.Shared.Return(buffer);
								stream.Dispose();
							}
							break;

						case MySqlBulkCopy bulkCopy:
							await bulkCopy.SendDataReaderAsync(ioBehavior, CancellationToken.None).ConfigureAwait(false);
							break;

						default:
							throw new InvalidOperationException("Unsupported Source type: {0}".FormatInvariant(source.GetType().Name));
						}
					}
					catch (Exception ex)
					{
						// store the exception, to be thrown after reading the response packet from the server
						ReadResultSetHeaderException = ExceptionDispatchInfo.Capture(new MySqlException("Error during LOAD DATA LOCAL INFILE", ex));
					}

					await Session.SendReplyAsync(EmptyPayload.Instance, ioBehavior, CancellationToken.None).ConfigureAwait(false);
				}
				else
				{
					static int ReadColumnCount(ReadOnlySpan<byte> span)
					{
						var reader = new ByteArrayReader(span);
						var columnCount_ = (int) reader.ReadLengthEncodedInteger();
						if (reader.BytesRemaining != 0)
							throw new MySqlException("Unexpected data at end of column_count packet; see https://github.com/mysql-net/MySqlConnector/issues/324");
						return columnCount_;
					}
					var columnCount = ReadColumnCount(payload.Span);

					// reserve adequate space to hold a copy of all column definitions (but note that this can be resized below if we guess too small)
					Utility.Resize(ref m_columnDefinitionPayloads, columnCount * 96);

					ColumnDefinitions = new ColumnDefinitionPayload[columnCount];
					ColumnTypes = new MySqlDbType[columnCount];

					for (var column = 0; column < ColumnDefinitions.Length; column++)
					{
						payload = await Session.ReceiveReplyAsync(ioBehavior, CancellationToken.None).ConfigureAwait(false);
						var payloadLength = payload.Span.Length;

						// 'Session.ReceiveReplyAsync' reuses a shared buffer; make a copy so that the column definitions can always be safely read at any future point
						if (m_columnDefinitionPayloadUsedBytes + payloadLength > m_columnDefinitionPayloads.Count)
							Utility.Resize(ref m_columnDefinitionPayloads, m_columnDefinitionPayloadUsedBytes + payloadLength);
						payload.Span.CopyTo(m_columnDefinitionPayloads.Array.AsSpan().Slice(m_columnDefinitionPayloadUsedBytes));

						var columnDefinition = ColumnDefinitionPayload.Create(new ResizableArraySegment<byte>(m_columnDefinitionPayloads, m_columnDefinitionPayloadUsedBytes, payloadLength));
						ColumnDefinitions[column] = columnDefinition;
						ColumnTypes[column] = TypeMapper.ConvertToMySqlDbType(columnDefinition, treatTinyAsBoolean: Connection.TreatTinyAsBoolean, guidFormat: Connection.GuidFormat);
						m_columnDefinitionPayloadUsedBytes += payloadLength;
					}

					if (!Session.SupportsDeprecateEof)
					{
						payload = await Session.ReceiveReplyAsync(ioBehavior, CancellationToken.None).ConfigureAwait(false);
						EofPayload.Create(payload.Span);
					}

					if (ColumnDefinitions.Length == (Command?.OutParameters?.Count + 1) && ColumnDefinitions[0].Name == SingleCommandPayloadCreator.OutParameterSentinelColumnName)
						ContainsCommandParameters = true;
					WarningCount = 0;
					State = ResultSetState.ReadResultSetHeader;
					if (DataReader.Activity is { IsAllDataRequested: true })
						DataReader.Activity.AddEvent(new ActivityEvent("read-result-set-header"));
					break;
				}
			}
		}
		catch (Exception ex)
		{
			ReadResultSetHeaderException = ExceptionDispatchInfo.Capture(ex);
		}
		finally
		{
			BufferState = State;
		}
	}

	private static bool IsHostVerified(MySqlConnection connection)
	{
		return connection.SslMode == MySqlSslMode.VerifyCA
			|| connection.SslMode == MySqlSslMode.VerifyFull;
	}

	public async Task ReadEntireAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		while (State is ResultSetState.ReadingRows or ResultSetState.ReadResultSetHeader)
			await ReadAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
	}

	public bool Read()
	{
		return ReadAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
	}

	public Task<bool> ReadAsync(CancellationToken cancellationToken) =>
		ReadAsync(Connection.AsyncIOBehavior, cancellationToken);

	public async Task<bool> ReadAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		m_row = m_readBuffer?.Count > 0 ? m_readBuffer.Dequeue() :
			await ScanRowAsync(ioBehavior, m_row, cancellationToken).ConfigureAwait(false);

		if (Command.ReturnParameter is not null && m_row is not null)
		{
			Command.ReturnParameter.Value = m_row.GetValue(0);
			Command.ReturnParameter = null;
		}

		if (m_row is null)
		{
			State = BufferState;
			return false;
		}
		State = ResultSetState.ReadingRows;
		return true;
	}

	public async Task<Row?> BufferReadAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		var row = await ScanRowAsync(ioBehavior, null, cancellationToken).ConfigureAwait(false);
		if (row is null)
			return null;
		m_readBuffer ??= new();
		m_readBuffer.Enqueue(row);
		return row;
	}

	private ValueTask<Row?> ScanRowAsync(IOBehavior ioBehavior, Row? row, CancellationToken cancellationToken)
	{
		// if we've already read past the end of this resultset, Read returns false
		if (BufferState is ResultSetState.HasMoreData or ResultSetState.NoMoreData or ResultSetState.None)
			return new ValueTask<Row?>(default(Row?));

		using var registration = Command.CancellableCommand.RegisterCancel(cancellationToken); // lgtm[cs/useless-assignment-to-local]
		var payloadValueTask = Session.ReceiveReplyAsync(ioBehavior, CancellationToken.None);
		return payloadValueTask.IsCompletedSuccessfully
			? new ValueTask<Row?>(ScanRowAsyncRemainder(this, payloadValueTask.Result, row))
			: new ValueTask<Row?>(ScanRowAsyncAwaited(this, payloadValueTask.AsTask(), row, cancellationToken));

		static async Task<Row?> ScanRowAsyncAwaited(ResultSet resultSet, Task<PayloadData> payloadTask, Row? row, CancellationToken token)
		{
			PayloadData payloadData;
			try
			{
				payloadData = await payloadTask.ConfigureAwait(false);
			}
			catch (MySqlException ex)
			{
				resultSet.BufferState = resultSet.State = ResultSetState.NoMoreData;
				if (ex.ErrorCode == MySqlErrorCode.QueryInterrupted && token.IsCancellationRequested)
					throw new OperationCanceledException(ex.Message, ex, token);
				if (ex.ErrorCode == MySqlErrorCode.QueryInterrupted && resultSet.Command.CancellableCommand.IsTimedOut)
					throw MySqlException.CreateForTimeout(ex);
				throw;
			}
			return ScanRowAsyncRemainder(resultSet, payloadData, row);
		}

		static Row? ScanRowAsyncRemainder(ResultSet resultSet, PayloadData payload, Row? row)
		{
			if (payload.HeaderByte == EofPayload.Signature)
			{
				var span = payload.Span;
				if (resultSet.Session.SupportsDeprecateEof && OkPayload.IsOk(span, resultSet.Session.SupportsDeprecateEof))
				{
					var ok = OkPayload.Create(span, resultSet.Session.SupportsDeprecateEof, resultSet.Session.SupportsSessionTrack);
					resultSet.BufferState = (ok.ServerStatus & ServerStatus.MoreResultsExist) == 0 ? ResultSetState.NoMoreData : ResultSetState.HasMoreData;
					return null;
				}
				if (!resultSet.Session.SupportsDeprecateEof && EofPayload.IsEof(payload))
				{
					var eof = EofPayload.Create(span);
					resultSet.BufferState = (eof.ServerStatus & ServerStatus.MoreResultsExist) == 0 ? ResultSetState.NoMoreData : ResultSetState.HasMoreData;
					return null;
				}
			}

			row ??= resultSet.Command.TryGetPreparedStatements() is null ? new TextRow(resultSet) : new BinaryRow(resultSet);
			row.SetData(payload.Memory);
			resultSet.m_hasRows = true;
			resultSet.BufferState = ResultSetState.ReadingRows;
			return row;
		}
	}

#pragma warning disable CA1822 // Mark members as static
	public int Depth => 0;
#pragma warning restore CA1822 // Mark members as static

	public string GetName(int ordinal)
	{
		if (ColumnDefinitions is null)
			throw new InvalidOperationException("There is no current result set.");
		if (ordinal < 0 || ordinal >= ColumnDefinitions.Length)
			throw new IndexOutOfRangeException("value must be between 0 and {0}".FormatInvariant(ColumnDefinitions.Length - 1));
		return ColumnDefinitions[ordinal].Name;
	}

	public string GetDataTypeName(int ordinal)
	{
		if (ColumnDefinitions is null)
			throw new InvalidOperationException("There is no current result set.");
		if (ordinal < 0 || ordinal >= ColumnDefinitions.Length)
			throw new IndexOutOfRangeException("value must be between 0 and {0}.".FormatInvariant(ColumnDefinitions.Length));

		var mySqlDbType = ColumnTypes![ordinal];
		if (mySqlDbType == MySqlDbType.String)
			return string.Format(CultureInfo.InvariantCulture, "CHAR({0})", ColumnDefinitions[ordinal].ColumnLength / ProtocolUtility.GetBytesPerCharacter(ColumnDefinitions[ordinal].CharacterSet));
		return TypeMapper.Instance.GetColumnTypeMetadata(mySqlDbType).SimpleDataTypeName;
	}

	public Type GetFieldType(int ordinal)
	{
		if (ColumnDefinitions is null)
			throw new InvalidOperationException("There is no current result set.");
		if (ordinal < 0 || ordinal >= ColumnDefinitions.Length)
			throw new IndexOutOfRangeException("value must be between 0 and {0}.".FormatInvariant(ColumnDefinitions.Length));

		var type = TypeMapper.Instance.GetColumnTypeMetadata(ColumnTypes![ordinal]).DbTypeMapping.ClrType;
		if (Connection.AllowZeroDateTime && type == typeof(DateTime))
			type = typeof(MySqlDateTime);
		return type;
	}

	public int FieldCount => ColumnDefinitions?.Length ?? 0;

	public bool HasRows
	{
		get
		{
			if (BufferState == ResultSetState.ReadResultSetHeader)
				return BufferReadAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult() is not null;
			return m_hasRows;
		}
	}

	public int GetOrdinal(string name)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		if (ColumnDefinitions is null)
			throw new InvalidOperationException("There is no current result set.");

		for (var column = 0; column < ColumnDefinitions.Length; column++)
		{
			if (name.Equals(ColumnDefinitions[column].Name, StringComparison.OrdinalIgnoreCase))
				return column;
		}

		throw new IndexOutOfRangeException("The column name '{0}' does not exist in the result set.".FormatInvariant(name));
	}

	public Row GetCurrentRow()
	{
		if (State != ResultSetState.ReadingRows)
			throw new InvalidOperationException("Read must be called first.");
		return m_row ?? throw new InvalidOperationException("There is no current row.");
	}

	public readonly MySqlDataReader DataReader;
	public ExceptionDispatchInfo? ReadResultSetHeaderException { get; private set; }
	public IMySqlCommand Command => DataReader.Command!;
	public MySqlConnection Connection => DataReader.Connection!;
	public ServerSession Session => DataReader.Session!;

	public ResultSetState BufferState { get; private set; }
	public ColumnDefinitionPayload[]? ColumnDefinitions { get; private set; }
	public MySqlDbType[]? ColumnTypes { get; private set; }
	public ulong? RecordsAffected { get; private set; }
	public int WarningCount { get; private set; }
	public ResultSetState State { get; private set; }
	public bool ContainsCommandParameters { get; private set; }

	ResizableArray<byte>? m_columnDefinitionPayloads;
	int m_columnDefinitionPayloadUsedBytes;
	Queue<Row>? m_readBuffer;
	Row? m_row;
	bool m_hasRows;
}
