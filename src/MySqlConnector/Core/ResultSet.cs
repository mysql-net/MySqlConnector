using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using MySql.Data.Types;
using MySqlConnector.Protocol;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core
{
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
			LastInsertId = 0;
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
						LastInsertId = unchecked((long) ok.LastInsertId);
						WarningCount = ok.WarningCount;
						if (ok.NewSchema is object)
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
							if (!IsHostVerified(Connection)
								&& !localInfile.FileName.StartsWith(MySqlBulkLoader.StreamPrefix, StringComparison.Ordinal))
								throw new NotSupportedException("Use SourceStream or SslMode >= VerifyCA for LOAD DATA LOCAL INFILE. See https://fl.vu/mysql-load-data");

							using var stream = localInfile.FileName.StartsWith(MySqlBulkLoader.StreamPrefix, StringComparison.Ordinal) ?
								MySqlBulkLoader.GetAndRemoveStream(localInfile.FileName) :
								File.OpenRead(localInfile.FileName);
							var readBuffer = new byte[65536];
							int byteCount;
							while ((byteCount = await stream.ReadAsync(readBuffer, 0, readBuffer.Length).ConfigureAwait(false)) > 0)
							{
								payload = new PayloadData(new ArraySegment<byte>(readBuffer, 0, byteCount));
								await Session.SendReplyAsync(payload, ioBehavior, CancellationToken.None).ConfigureAwait(false);
							}
						}
						catch (Exception ex)
						{
							// store the exception, to be thrown after reading the response packet from the server
							ReadResultSetHeaderException = new MySqlException("Error during LOAD DATA LOCAL INFILE", ex);
						}

						await Session.SendReplyAsync(EmptyPayload.Instance, ioBehavior, CancellationToken.None).ConfigureAwait(false);
					}
					else
					{
						int ReadColumnCount(ReadOnlySpan<byte> span)
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
						LastInsertId = -1;
						WarningCount = 0;
						State = ResultSetState.ReadResultSetHeader;
						break;
					}
				}
			}
			catch (Exception ex)
			{
				ReadResultSetHeaderException = ex;
			}
			finally
			{
				BufferState = State;
			}
		}

		private bool IsHostVerified(MySqlConnection connection)
		{
			return connection.SslMode == MySqlSslMode.VerifyCA
				|| connection.SslMode == MySqlSslMode.VerifyFull;
		}

		public async Task ReadEntireAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			while (State == ResultSetState.ReadingRows || State == ResultSetState.ReadResultSetHeader)
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

			if (Command.ReturnParameter is object && m_row is object)
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
			m_readBuffer ??= new Queue<Row>();
			m_readBuffer.Enqueue(row);
			return row;
		}

		private ValueTask<Row?> ScanRowAsync(IOBehavior ioBehavior, Row? row, CancellationToken cancellationToken)
		{
			// if we've already read past the end of this resultset, Read returns false
			if (BufferState == ResultSetState.HasMoreData || BufferState == ResultSetState.NoMoreData || BufferState == ResultSetState.None)
				return new ValueTask<Row?>(default(Row?));

			using var registration = Command.CancellableCommand.RegisterCancel(cancellationToken);
			var payloadValueTask = Session.ReceiveReplyAsync(ioBehavior, CancellationToken.None);
			return payloadValueTask.IsCompletedSuccessfully
				? new ValueTask<Row?>(ScanRowAsyncRemainder(this, payloadValueTask.Result, row))
				: new ValueTask<Row?>(ScanRowAsyncAwaited(this, payloadValueTask.AsTask(), row, cancellationToken));

			static async Task<Row?> ScanRowAsyncAwaited(ResultSet this_, Task<PayloadData> payloadTask, Row? row_, CancellationToken token)
			{
				PayloadData payloadData;
				try
				{
					payloadData = await payloadTask.ConfigureAwait(false);
				}
				catch (MySqlException ex)
				{
					this_.BufferState = this_.State = ResultSetState.NoMoreData;
					if (ex.Number == (int) MySqlErrorCode.QueryInterrupted)
						token.ThrowIfCancellationRequested();
					throw;
				}
				return ScanRowAsyncRemainder(this_, payloadData, row_);
			}

			static Row? ScanRowAsyncRemainder(ResultSet this_, PayloadData payload, Row? row_)
			{
				if (payload.HeaderByte == EofPayload.Signature)
				{
					var span = payload.Span;
					if (this_.Session.SupportsDeprecateEof && OkPayload.IsOk(span, this_.Session.SupportsDeprecateEof))
					{
						var ok = OkPayload.Create(span, this_.Session.SupportsDeprecateEof, this_.Session.SupportsSessionTrack);
						this_.BufferState = (ok.ServerStatus & ServerStatus.MoreResultsExist) == 0 ? ResultSetState.NoMoreData : ResultSetState.HasMoreData;
						return null;
					}
					if (!this_.Session.SupportsDeprecateEof && EofPayload.IsEof(payload))
					{
						var eof = EofPayload.Create(span);
						this_.BufferState = (eof.ServerStatus & ServerStatus.MoreResultsExist) == 0 ? ResultSetState.NoMoreData : ResultSetState.HasMoreData;
						return null;
					}
				}

				if (row_ is null)
				{
					bool isBinaryRow = false;
					if (payload.HeaderByte == 0 && !this_.Connection.IgnorePrepare)
					{
						// this might be a binary row, but it might also be a text row whose first column is zero bytes long; try reading
						// the row as a series of length-encoded values (the text format) to see if this might plausibly be a text row
						var isTextRow = false;
						var reader = new ByteArrayReader(payload.Span);
						var columnCount = 0;
						while (reader.BytesRemaining > 0)
						{
							int length;
							var firstByte = reader.ReadByte();
							if (firstByte == 0xFB)
							{
								// NULL
								length = 0;
							}
							else if (firstByte == 0xFC)
							{
								// two-byte length-encoded integer
								if (reader.BytesRemaining < 2)
									break;
								length = unchecked((int) reader.ReadFixedLengthUInt32(2));
							}
							else if (firstByte == 0xFD)
							{
								// three-byte length-encoded integer
								if (reader.BytesRemaining < 3)
									break;
								length = unchecked((int) reader.ReadFixedLengthUInt32(3));
							}
							else if (firstByte == 0xFE)
							{
								// eight-byte length-encoded integer
								if (reader.BytesRemaining < 8)
									break;
								length = checked((int) reader.ReadFixedLengthUInt64(8));
							}
							else if (firstByte == 0xFF)
							{
								// invalid length prefix
								break;
							}
							else
							{
								// single-byte length
								length = firstByte;
							}

							if (reader.BytesRemaining < length)
								break;
							reader.Offset += length;
							columnCount++;

							if (columnCount == this_.ColumnDefinitions!.Length)
							{
								// if we used up all the bytes reading exactly 'ColumnDefinitions' length-encoded columns, then assume this is a text row
								if (reader.BytesRemaining == 0)
									isTextRow = true;
								break;
							}
						}

						isBinaryRow = !isTextRow;
					}
					row_ = isBinaryRow ? (Row) new BinaryRow(this_) : new TextRow(this_);
				}
				row_.SetData(payload.Memory);
				this_.m_hasRows = true;
				this_.BufferState = ResultSetState.ReadingRows;
				return row_;
			}
		}

		public int Depth => 0;

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
					return BufferReadAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult() is object;
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
		public Exception? ReadResultSetHeaderException { get; private set; }
		public IMySqlCommand Command => DataReader.Command;
		public MySqlConnection Connection => DataReader.Connection;
		public ServerSession Session => DataReader.Session;

		public ResultSetState BufferState { get; private set; }
		public ColumnDefinitionPayload[]? ColumnDefinitions { get; private set; }
		public MySqlDbType[]? ColumnTypes { get; private set; }
		public long LastInsertId { get; private set; }
		public int? RecordsAffected { get; private set; }
		public int WarningCount { get; private set; }
		public ResultSetState State { get; private set; }
		public bool ContainsCommandParameters { get; private set; }

		ResizableArray<byte>? m_columnDefinitionPayloads;
		int m_columnDefinitionPayloadUsedBytes;
		Queue<Row>? m_readBuffer;
		Row? m_row;
		bool m_hasRows;
	}
}
