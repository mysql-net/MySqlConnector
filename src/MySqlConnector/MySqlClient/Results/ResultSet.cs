using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient.Types;
using MySql.Data.Protocol.Serialization;
using MySql.Data.Serialization;

namespace MySql.Data.MySqlClient.Results
{
	internal class ResultSet
	{
		public ResultSet(MySqlDataReader dataReader)
		{
			DataReader = dataReader;
		}

		public async Task<ResultSet> ReadResultSetHeaderAsync(IOBehavior ioBehavior)
		{
			// ResultSet can be re-used, so initialize everything
			BufferState = ResultSetState.None;
			ColumnDefinitions = null;
			ColumnTypes = null;
			LastInsertId = 0;
			RecordsAffected = 0;
			State = ResultSetState.None;
			m_columnDefinitionPayloadUsedBytes = 0;
			m_dataLengths = null;
			m_dataOffsets = null;
			m_readBuffer.Clear();
			m_row = null;
			m_rowBuffered = null;
			m_hasRows = false;

			try
			{
				while (true)
				{
					var payload = await Session.ReceiveReplyAsync(ioBehavior, CancellationToken.None).ConfigureAwait(false);

					var firstByte = payload.HeaderByte;
					if (firstByte == OkPayload.Signature)
					{
						var ok = OkPayload.Create(payload);
						RecordsAffected += ok.AffectedRowCount;
						LastInsertId = ok.LastInsertId;
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
							var localInfile = LocalInfilePayload.Create(payload);
							if(!IsHostVerified(Connection)
								&& !localInfile.FileName.StartsWith(MySqlBulkLoader.StreamPrefix, StringComparison.Ordinal))
								throw new NotSupportedException("Use SourceStream or SslMode >= VerifyCA for LOAD DATA LOCAL INFILE");

							using (var stream = localInfile.FileName.StartsWith(MySqlBulkLoader.StreamPrefix, StringComparison.Ordinal) ?
								MySqlBulkLoader.GetAndRemoveStream(localInfile.FileName) :
								File.OpenRead(localInfile.FileName))
							{
								byte[] readBuffer = new byte[65536];
								int byteCount;
								while ((byteCount = await stream.ReadAsync(readBuffer, 0, readBuffer.Length).ConfigureAwait(false)) > 0)
								{
									payload = new PayloadData(new ArraySegment<byte>(readBuffer, 0, byteCount));
									await Session.SendReplyAsync(payload, ioBehavior, CancellationToken.None).ConfigureAwait(false);
								}
							}
						}
						catch (Exception ex)
						{
							// store the exception, to be thrown after reading the response packet from the server
							ReadResultSetHeaderException = new MySqlException("Error during LOAD DATA LOCAL INFILE", ex);
						}

						await Session.SendReplyAsync(EmptyPayload.Create(), ioBehavior, CancellationToken.None).ConfigureAwait(false);
					}
					else
					{
						var reader = new ByteArrayReader(payload.ArraySegment);
						var columnCount = (int) reader.ReadLengthEncodedInteger();
						if (reader.BytesRemaining != 0)
							throw new MySqlException("Unexpected data at end of column_count packet; see https://github.com/mysql-net/MySqlConnector/issues/324");

						// reserve adequate space to hold a copy of all column definitions (but note that this can be resized below if we guess too small)
						Array.Resize(ref m_columnDefinitionPayloads, columnCount * 96);

						ColumnDefinitions = new ColumnDefinitionPayload[columnCount];
						ColumnTypes = new MySqlDbType[columnCount];
						m_dataOffsets = new int[columnCount];
						m_dataLengths = new int[columnCount];

						for (var column = 0; column < ColumnDefinitions.Length; column++)
						{
							payload = await Session.ReceiveReplyAsync(ioBehavior, CancellationToken.None).ConfigureAwait(false);
							var arraySegment = payload.ArraySegment;

							// 'Session.ReceiveReplyAsync' reuses a shared buffer; make a copy so that the column definitions can always be safely read at any future point
							if (m_columnDefinitionPayloadUsedBytes + arraySegment.Count > m_columnDefinitionPayloads.Length)
								Array.Resize(ref m_columnDefinitionPayloads, Math.Max(m_columnDefinitionPayloadUsedBytes + arraySegment.Count, m_columnDefinitionPayloadUsedBytes * 2));
							Buffer.BlockCopy(arraySegment.Array, arraySegment.Offset, m_columnDefinitionPayloads, m_columnDefinitionPayloadUsedBytes, arraySegment.Count);

							var columnDefinition = ColumnDefinitionPayload.Create(new ArraySegment<byte>(m_columnDefinitionPayloads, m_columnDefinitionPayloadUsedBytes, arraySegment.Count));
							ColumnDefinitions[column] = columnDefinition;
							ColumnTypes[column] = TypeMapper.ConvertToMySqlDbType(columnDefinition, treatTinyAsBoolean: Connection.TreatTinyAsBoolean, oldGuids: Connection.OldGuids);
							m_columnDefinitionPayloadUsedBytes += arraySegment.Count;
						}

						if (!Session.SupportsDeprecateEof)
						{
							payload = await Session.ReceiveReplyAsync(ioBehavior, CancellationToken.None).ConfigureAwait(false);
							EofPayload.Create(payload);
						}

						LastInsertId = -1;
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

			return this;
		}

		private bool IsHostVerified(MySqlConnection connection)
		{
			return connection.SslMode == MySqlSslMode.VerifyCA
				|| connection.SslMode == MySqlSslMode.VerifyFull;
		}

		public async Task BufferEntireAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			while (BufferState == ResultSetState.ReadingRows || BufferState == ResultSetState.ReadResultSetHeader)
				await BufferReadAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
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
			ReadAsync(Command.Connection.AsyncIOBehavior, cancellationToken);

		public async Task<bool> ReadAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			m_row?.ClearData();
			m_row = m_readBuffer.Count > 0
				? m_readBuffer.Dequeue()
				: await ScanRowAsync(ioBehavior, m_row, cancellationToken).ConfigureAwait(false);

			if (m_row == null)
			{
				State = BufferState;
				return false;
			}
			State = ResultSetState.ReadingRows;
			return true;
		}

		public async Task<Row> BufferReadAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			m_rowBuffered?.BufferData();
			// ScanRowAsync sets m_rowBuffered to the next row if there is one
			if (await ScanRowAsync(ioBehavior, null, cancellationToken).ConfigureAwait(false) == null)
				return null;
			m_readBuffer.Enqueue(m_rowBuffered);
			return m_rowBuffered;
		}

		private ValueTask<Row> ScanRowAsync(IOBehavior ioBehavior, Row row, CancellationToken cancellationToken)
		{
			// if we've already read past the end of this resultset, Read returns false
			if (BufferState == ResultSetState.HasMoreData || BufferState == ResultSetState.NoMoreData || BufferState == ResultSetState.None)
				return new ValueTask<Row>((Row) null);

			using (Command.RegisterCancel(cancellationToken))
			{
				var payloadValueTask = Session.ReceiveReplyAsync(ioBehavior, CancellationToken.None);
				return payloadValueTask.IsCompletedSuccessfully
					? new ValueTask<Row>(ScanRowAsyncRemainder(payloadValueTask.Result, row))
					: new ValueTask<Row>(ScanRowAsyncAwaited(payloadValueTask.AsTask(), row, cancellationToken));
			}

			async Task<Row> ScanRowAsyncAwaited(Task<PayloadData> payloadTask, Row row_, CancellationToken token)
			{
				PayloadData payloadData;
				try
				{
					payloadData = await payloadTask.ConfigureAwait(false);
				}
				catch (MySqlException ex)
				{
					BufferState = State = ResultSetState.NoMoreData;
					if (ex.Number == (int) MySqlErrorCode.QueryInterrupted)
						token.ThrowIfCancellationRequested();
					throw;
				}
				return ScanRowAsyncRemainder(payloadData, row_);
			}

			Row ScanRowAsyncRemainder(PayloadData payload, Row row_)
			{
				if (payload.HeaderByte == EofPayload.Signature)
				{
					if (Session.SupportsDeprecateEof && OkPayload.IsOk(payload, Session.SupportsDeprecateEof))
					{
						var ok = OkPayload.Create(payload, Session.SupportsDeprecateEof);
						BufferState = (ok.ServerStatus & ServerStatus.MoreResultsExist) == 0 ? ResultSetState.NoMoreData : ResultSetState.HasMoreData;
						m_rowBuffered = null;
						return null;
					}
					if (!Session.SupportsDeprecateEof && EofPayload.IsEof(payload))
					{
						var eof = EofPayload.Create(payload);
						BufferState = (eof.ServerStatus & ServerStatus.MoreResultsExist) == 0 ? ResultSetState.NoMoreData : ResultSetState.HasMoreData;
						m_rowBuffered = null;
						return null;
					}
				}

				var reader = new ByteArrayReader(payload.ArraySegment);
				for (var column = 0; column < m_dataOffsets.Length; column++)
				{
					var length = reader.ReadLengthEncodedIntegerOrNull();
					m_dataLengths[column] = length == -1 ? 0 : length;
					m_dataOffsets[column] = length == -1 ? -1 : reader.Offset;
					reader.Offset += m_dataLengths[column];
				}

				if (row_ == null)
					row_ = new Row(this);
				row_.SetData(m_dataLengths, m_dataOffsets, payload.ArraySegment);
				m_rowBuffered = row_;
				m_hasRows = true;
				BufferState = ResultSetState.ReadingRows;
				return row_;
			}
		}

		public string GetName(int ordinal)
		{
			if (ColumnDefinitions == null)
				throw new IndexOutOfRangeException("There is no current result set.");
			if (ordinal < 0 || ordinal > ColumnDefinitions.Length)
				throw new IndexOutOfRangeException("value must be between 0 and {0}".FormatInvariant(ColumnDefinitions.Length - 1));
			return ColumnDefinitions[ordinal].Name;
		}

		public string GetDataTypeName(int ordinal)
		{
			if (ordinal < 0 || ordinal > ColumnDefinitions.Length)
				throw new ArgumentOutOfRangeException(nameof(ordinal), "value must be between 0 and {0}.".FormatInvariant(ColumnDefinitions.Length));

			switch (ColumnTypes[ordinal])
			{
			case MySqlDbType.Bool:
				return "BOOL";

			case MySqlDbType.UByte:
			case MySqlDbType.Byte:
				return "TINYINT";

			case MySqlDbType.UInt16:
			case MySqlDbType.Int16:
				return "SMALLINT";

			case MySqlDbType.UInt24:
			case MySqlDbType.Int24:
				return "MEDIUMINT";

			case MySqlDbType.UInt32:
			case MySqlDbType.Int32:
				return "INT";

			case MySqlDbType.UInt64:
			case MySqlDbType.Int64:
				return "BIGINT";

			case MySqlDbType.Bit:
				return "BIT";

			case MySqlDbType.Enum:
				return "ENUM";

			case MySqlDbType.Set:
				return "SET";

			case MySqlDbType.Guid:
				return "CHAR(36)";

			case MySqlDbType.String:
				var columnDefinition = ColumnDefinitions[ordinal];
				return string.Format(CultureInfo.InvariantCulture, "CHAR({0})", columnDefinition.ColumnLength / SerializationUtility.GetBytesPerCharacter(columnDefinition.CharacterSet));

			case MySqlDbType.VarString:
			case MySqlDbType.TinyText:
			case MySqlDbType.Text:
			case MySqlDbType.MediumText:
			case MySqlDbType.LongText:
				return "VARCHAR";

			case MySqlDbType.Binary:
			case MySqlDbType.VarBinary:
			case MySqlDbType.TinyBlob:
			case MySqlDbType.Blob:
			case MySqlDbType.MediumBlob:
			case MySqlDbType.LongBlob:
				return "BLOB";

			case MySqlDbType.Date:
				return "DATE";

			case MySqlDbType.DateTime:
				return "DATETIME";

			case MySqlDbType.Timestamp:
				return "TIMESTAMP";

			case MySqlDbType.Time:
				return "TIME";

			case MySqlDbType.Year:
				return "YEAR";

			case MySqlDbType.Float:
				return "FLOAT";

			case MySqlDbType.Double:
				return "DOUBLE";

			case MySqlDbType.Decimal:
			case MySqlDbType.NewDecimal:
				return "DECIMAL";

			case MySqlDbType.JSON:
				return "JSON";

			case MySqlDbType.Null:
				// not a valid data type name, but only happens when there is no way to infer the type of the column, e.g., "SELECT NULL;"
				return "NULL";

			default:
				throw new NotImplementedException("GetDataTypeName for {0} is not implemented".FormatInvariant(ColumnTypes[ordinal]));
			}
		}

		public Type GetFieldType(int ordinal)
		{
			if (ordinal < 0 || ordinal > ColumnDefinitions.Length)
				throw new ArgumentOutOfRangeException(nameof(ordinal), "value must be between 0 and {0}.".FormatInvariant(ColumnDefinitions.Length));

			var columnDefinition = ColumnDefinitions[ordinal];
			var isUnsigned = (columnDefinition.ColumnFlags & ColumnFlags.Unsigned) != 0;
			switch (columnDefinition.ColumnType)
			{
				case ColumnType.Tiny:
					return Connection.TreatTinyAsBoolean && columnDefinition.ColumnLength == 1 ? typeof(bool) :
						isUnsigned ? typeof(byte) : typeof(sbyte);

				case ColumnType.Int24:
				case ColumnType.Long:
					return isUnsigned ? typeof(uint) : typeof(int);

				case ColumnType.Longlong:
					return isUnsigned ? typeof(ulong) : typeof(long);

				case ColumnType.Bit:
					return typeof(ulong);

				case ColumnType.String:
					if (!Connection.OldGuids && columnDefinition.ColumnLength / SerializationUtility.GetBytesPerCharacter(columnDefinition.CharacterSet) == 36)
						return typeof(Guid);
					goto case ColumnType.VarString;

				case ColumnType.VarString:
				case ColumnType.TinyBlob:
				case ColumnType.Blob:
				case ColumnType.MediumBlob:
				case ColumnType.LongBlob:
					return columnDefinition.CharacterSet == CharacterSet.Binary ?
						(Connection.OldGuids && columnDefinition.ColumnLength == 16 ? typeof(Guid) : typeof(byte[])) :
						typeof(string);

				case ColumnType.Json:
					return typeof(string);

				case ColumnType.Short:
					return isUnsigned ? typeof(ushort) : typeof(short);

				case ColumnType.Date:
				case ColumnType.DateTime:
				case ColumnType.Timestamp:
					return typeof(DateTime);

				case ColumnType.Time:
					return typeof(TimeSpan);

				case ColumnType.Year:
					return typeof(int);

				case ColumnType.Float:
					return typeof(float);

				case ColumnType.Double:
					return typeof(double);

				case ColumnType.Decimal:
				case ColumnType.NewDecimal:
					return typeof(decimal);

				case ColumnType.Null:
					return typeof(object);

				default:
					throw new NotImplementedException("GetFieldType for {0} is not implemented".FormatInvariant(columnDefinition.ColumnType));
			}
		}

		public int FieldCount => ColumnDefinitions?.Length ?? 0;

		public bool HasRows
		{
			get
			{
				if (BufferState == ResultSetState.ReadResultSetHeader)
					return BufferReadAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult() != null;
				return m_hasRows;
			}
		}

		public Row GetCurrentRow()
		{
			if (State != ResultSetState.ReadingRows)
				throw new InvalidOperationException("Read must be called first.");
			return m_row ?? throw new InvalidOperationException("There is no current row.");
		}

		public readonly MySqlDataReader DataReader;
		public Exception ReadResultSetHeaderException { get; private set; }
		public MySqlCommand Command => DataReader.Command;
		public MySqlConnection Connection => DataReader.Connection;
		public MySqlSession Session => DataReader.Session;

		public ResultSetState BufferState { get; private set; }
		public ColumnDefinitionPayload[] ColumnDefinitions { get; private set; }
		public MySqlDbType[] ColumnTypes { get; private set; }
		public long LastInsertId { get; private set; }
		public int RecordsAffected { get; private set; }
		public ResultSetState State { get; private set; }

		byte[] m_columnDefinitionPayloads;
		int m_columnDefinitionPayloadUsedBytes;
		int[] m_dataLengths;
		int[] m_dataOffsets;
		readonly Queue<Row> m_readBuffer = new Queue<Row>();
		Row m_row;
		Row m_rowBuffered;
		bool m_hasRows;
	}
}
