using System.Buffers;
using System.Buffers.Text;
using System.Globalization;
using System.Numerics;
using System.Text;
using Microsoft.Extensions.Logging;
using MySqlConnector.Core;
using MySqlConnector.Logging;
using MySqlConnector.Protocol;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector;

/// <summary>
/// <para><see cref="MySqlBulkCopy"/> lets you efficiently load a MySQL Server table with data from another source.
/// It is similar to the <a href="https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlbulkcopy">SqlBulkCopy</a> class
/// for SQL Server.</para>
/// <para>Due to <a href="https://mysqlconnector.net/troubleshooting/load-data-local-infile/">security features</a>
/// in MySQL Server, the connection string <em>must</em> have <c>AllowLoadLocalInfile=true</c> in order
/// to use this class.</para>
/// <para>For data that is in CSV or TSV format, use <see cref="MySqlBulkLoader"/> to bulk load the file.</para>
/// <para>Example code:</para>
/// <code>
/// // NOTE: to copy data between tables in the same database, use INSERT ... SELECT
/// // https://dev.mysql.com/doc/refman/8.0/en/insert-select.html
/// var dataTable = GetDataTableFromExternalSource();
///
/// // open the connection
/// using var connection = new MySqlConnection("...;AllowLoadLocalInfile=True");
/// await connection.OpenAsync();
///
/// // bulk copy the data
/// var bulkCopy = new MySqlBulkCopy(connection);
/// bulkCopy.DestinationTableName = "some_table_name";
/// var result = await bulkCopy.WriteToServerAsync(dataTable);
///
/// // check for problems
/// if (result.Warnings.Count != 0) { /* handle potential data loss warnings */ }
/// </code>
/// </summary>
/// <remarks><para><strong>Note:</strong> This API is a unique feature of MySqlConnector; you must
/// <a href="https://mysqlconnector.net/overview/installing/">switch to MySqlConnector</a> in order to use it.</para>
/// <para>This API is experimental and may change in the future.</para>
/// </remarks>
public sealed class MySqlBulkCopy
{
	/// <summary>
	/// Initializes a <see cref="MySqlBulkCopy"/> object with the specified connection, and optionally the active transaction.
	/// </summary>
	/// <param name="connection">The <see cref="MySqlConnection"/> to use.</param>
	/// <param name="transaction">(Optional) The <see cref="MySqlTransaction"/> to use.</param>
	public MySqlBulkCopy(MySqlConnection connection, MySqlTransaction? transaction = null)
	{
		m_connection = connection ?? throw new ArgumentNullException(nameof(connection));
		m_transaction = transaction;
		m_logger = m_connection.LoggingConfiguration.BulkCopyLogger;
		ColumnMappings = new();
	}

	/// <summary>
	/// A <see cref="MySqlBulkLoaderConflictOption"/> value that specifies how conflicts are resolved (default <see cref="MySqlBulkLoaderConflictOption.None"/>).
	/// </summary>
	public MySqlBulkLoaderConflictOption ConflictOption { get; set; }

	/// <summary>
	/// The number of seconds for the operation to complete before it times out, or <c>0</c> for no timeout.
	/// </summary>
	public int BulkCopyTimeout { get; set; }

	/// <summary>
	/// The name of the table to insert rows into.
	/// </summary>
	/// <remarks>This name needs to be quoted if it contains special characters.</remarks>
	public string? DestinationTableName { get; set; }

	/// <summary>
	/// If non-zero, this specifies the number of rows to be processed before generating a notification event.
	/// </summary>
	public int NotifyAfter { get; set; }

	/// <summary>
	/// This event is raised every time that the number of rows specified by the <see cref="NotifyAfter"/> property have been processed.
	/// </summary>
	/// <remarks>
	/// <para>Receipt of a RowsCopied event does not imply that any rows have been sent to the server or committed.</para>
	/// <para>The <see cref="MySqlRowsCopiedEventArgs.Abort"/> property can be set to <c>true</c> by the event handler to abort the copy.</para>
	/// </remarks>
	public event MySqlRowsCopiedEventHandler? MySqlRowsCopied;

	/// <summary>
	/// A collection of <see cref="MySqlBulkCopyColumnMapping"/> objects. If the columns being copied from the
	/// data source line up one-to-one with the columns in the destination table then populating this collection is
	/// unnecessary. Otherwise, this should be filled with a collection of <see cref="MySqlBulkCopyColumnMapping"/> objects
	/// specifying how source columns are to be mapped onto destination columns. If one column mapping is specified,
	/// then all must be specified.
	/// </summary>
	public List<MySqlBulkCopyColumnMapping> ColumnMappings { get; }

	/// <summary>
	/// Returns the number of rows that were copied (after <c>WriteToServer(Async)</c> finishes).
	/// </summary>
	[Obsolete("Use the MySqlBulkCopyResult.RowsInserted property returned by WriteToServer.")]
	public int RowsCopied => m_rowsCopied;

	/// <summary>
	/// Copies all rows in the supplied <see cref="DataTable"/> to the destination table specified by the
	/// <see cref="DestinationTableName"/> property of the <see cref="MySqlBulkCopy"/> object.
	/// </summary>
	/// <param name="dataTable">The <see cref="DataTable"/> to copy.</param>
	/// <returns>A <see cref="MySqlBulkCopyResult"/> with the result of the bulk copy operation.</returns>
	public MySqlBulkCopyResult WriteToServer(DataTable dataTable)
	{
		m_valuesEnumerator = DataRowsValuesEnumerator.Create(dataTable ?? throw new ArgumentNullException(nameof(dataTable)));
#pragma warning disable CA2012 // Safe because method completes synchronously
		return WriteToServerAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore CA2012
	}

#if NETCOREAPP || NETSTANDARD2_0_OR_GREATER
	/// <summary>
	/// Asynchronously copies all rows in the supplied <see cref="DataTable"/> to the destination table specified by the
	/// <see cref="DestinationTableName"/> property of the <see cref="MySqlBulkCopy"/> object.
	/// </summary>
	/// <param name="dataTable">The <see cref="DataTable"/> to copy.</param>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>A <see cref="MySqlBulkCopyResult"/> with the result of the bulk copy operation.</returns>
	public async ValueTask<MySqlBulkCopyResult> WriteToServerAsync(DataTable dataTable, CancellationToken cancellationToken = default)
	{
		m_valuesEnumerator = DataRowsValuesEnumerator.Create(dataTable ?? throw new ArgumentNullException(nameof(dataTable)));
		return await WriteToServerAsync(IOBehavior.Asynchronous, cancellationToken).ConfigureAwait(false);
	}
#else
	/// <summary>
	/// Asynchronously copies all rows in the supplied <see cref="DataTable"/> to the destination table specified by the
	/// <see cref="DestinationTableName"/> property of the <see cref="MySqlBulkCopy"/> object.
	/// </summary>
	/// <param name="dataTable">The <see cref="DataTable"/> to copy.</param>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>A <see cref="MySqlBulkCopyResult"/> with the result of the bulk copy operation.</returns>
	public async Task<MySqlBulkCopyResult> WriteToServerAsync(DataTable dataTable, CancellationToken cancellationToken = default)
	{
		m_valuesEnumerator = DataRowsValuesEnumerator.Create(dataTable ?? throw new ArgumentNullException(nameof(dataTable)));
		return await WriteToServerAsync(IOBehavior.Asynchronous, cancellationToken).ConfigureAwait(false);
	}
#endif

	/// <summary>
	/// Copies all rows in the supplied sequence of <see cref="DataRow"/> objects to the destination table specified by the
	/// <see cref="DestinationTableName"/> property of the <see cref="MySqlBulkCopy"/> object. The number of columns
	/// to be read from the <see cref="DataRow"/> objects must be specified in advance.
	/// </summary>
	/// <param name="dataRows">The collection of <see cref="DataRow"/> objects.</param>
	/// <param name="columnCount">The number of columns to copy (in each row).</param>
	/// <returns>A <see cref="MySqlBulkCopyResult"/> with the result of the bulk copy operation.</returns>
	public MySqlBulkCopyResult WriteToServer(IEnumerable<DataRow> dataRows, int columnCount)
	{
		m_valuesEnumerator = new DataRowsValuesEnumerator(dataRows ?? throw new ArgumentNullException(nameof(dataRows)), columnCount);
#pragma warning disable CA2012 // Safe because method completes synchronously
		return WriteToServerAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore CA2012
	}

#if NETCOREAPP || NETSTANDARD2_0_OR_GREATER
	/// <summary>
	/// Asynchronously copies all rows in the supplied sequence of <see cref="DataRow"/> objects to the destination table specified by the
	/// <see cref="DestinationTableName"/> property of the <see cref="MySqlBulkCopy"/> object. The number of columns
	/// to be read from the <see cref="DataRow"/> objects must be specified in advance.
	/// </summary>
	/// <param name="dataRows">The collection of <see cref="DataRow"/> objects.</param>
	/// <param name="columnCount">The number of columns to copy (in each row).</param>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>A <see cref="MySqlBulkCopyResult"/> with the result of the bulk copy operation.</returns>
	public async ValueTask<MySqlBulkCopyResult> WriteToServerAsync(IEnumerable<DataRow> dataRows, int columnCount, CancellationToken cancellationToken = default)
	{
		m_valuesEnumerator = new DataRowsValuesEnumerator(dataRows ?? throw new ArgumentNullException(nameof(dataRows)), columnCount);
		return await WriteToServerAsync(IOBehavior.Asynchronous, cancellationToken).ConfigureAwait(false);
	}
#else
	/// <summary>
	/// Asynchronously copies all rows in the supplied sequence of <see cref="DataRow"/> objects to the destination table specified by the
	/// <see cref="DestinationTableName"/> property of the <see cref="MySqlBulkCopy"/> object. The number of columns
	/// to be read from the <see cref="DataRow"/> objects must be specified in advance.
	/// </summary>
	/// <param name="dataRows">The collection of <see cref="DataRow"/> objects.</param>
	/// <param name="columnCount">The number of columns to copy (in each row).</param>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>A <see cref="MySqlBulkCopyResult"/> with the result of the bulk copy operation.</returns>
	public async Task<MySqlBulkCopyResult> WriteToServerAsync(IEnumerable<DataRow> dataRows, int columnCount, CancellationToken cancellationToken = default)
	{
		m_valuesEnumerator = new DataRowsValuesEnumerator(dataRows ?? throw new ArgumentNullException(nameof(dataRows)), columnCount);
		return await WriteToServerAsync(IOBehavior.Asynchronous, cancellationToken).ConfigureAwait(false);
	}
#endif

	/// <summary>
	/// Copies all rows in the supplied <see cref="IDataReader"/> to the destination table specified by the
	/// <see cref="DestinationTableName"/> property of the <see cref="MySqlBulkCopy"/> object.
	/// </summary>
	/// <param name="dataReader">The <see cref="IDataReader"/> to copy from.</param>
	/// <returns>A <see cref="MySqlBulkCopyResult"/> with the result of the bulk copy operation.</returns>
	public MySqlBulkCopyResult WriteToServer(IDataReader dataReader)
	{
		m_valuesEnumerator = DataReaderValuesEnumerator.Create(dataReader ?? throw new ArgumentNullException(nameof(dataReader)));
#pragma warning disable CA2012 // Safe because method completes synchronously
		return WriteToServerAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore CA2012
	}

#if NETCOREAPP || NETSTANDARD2_0_OR_GREATER
	/// <summary>
	/// Asynchronously copies all rows in the supplied <see cref="IDataReader"/> to the destination table specified by the
	/// <see cref="DestinationTableName"/> property of the <see cref="MySqlBulkCopy"/> object.
	/// </summary>
	/// <param name="dataReader">The <see cref="IDataReader"/> to copy from.</param>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>A <see cref="MySqlBulkCopyResult"/> with the result of the bulk copy operation.</returns>
	public async ValueTask<MySqlBulkCopyResult> WriteToServerAsync(IDataReader dataReader, CancellationToken cancellationToken = default)
	{
		m_valuesEnumerator = DataReaderValuesEnumerator.Create(dataReader ?? throw new ArgumentNullException(nameof(dataReader)));
		return await WriteToServerAsync(IOBehavior.Asynchronous, cancellationToken).ConfigureAwait(false);
	}
#else
	/// <summary>
	/// Asynchronously copies all rows in the supplied <see cref="IDataReader"/> to the destination table specified by the
	/// <see cref="DestinationTableName"/> property of the <see cref="MySqlBulkCopy"/> object.
	/// </summary>
	/// <param name="dataReader">The <see cref="IDataReader"/> to copy from.</param>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>A <see cref="MySqlBulkCopyResult"/> with the result of the bulk copy operation.</returns>
	public async Task<MySqlBulkCopyResult> WriteToServerAsync(IDataReader dataReader, CancellationToken cancellationToken = default)
	{
		m_valuesEnumerator = DataReaderValuesEnumerator.Create(dataReader ?? throw new ArgumentNullException(nameof(dataReader)));
		return await WriteToServerAsync(IOBehavior.Asynchronous, cancellationToken).ConfigureAwait(false);
	}
#endif

	private async ValueTask<MySqlBulkCopyResult> WriteToServerAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		var tableName = DestinationTableName ?? throw new InvalidOperationException("DestinationTableName must be set before calling WriteToServer");
		m_wasAborted = false;

		Log.StartingBulkCopy(m_logger, tableName);
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
			Timeout = BulkCopyTimeout,
			ConflictOption = ConflictOption,
		};

		var closeConnection = false;
		if (m_connection.State != ConnectionState.Open)
		{
			m_connection.Open();
			closeConnection = true;
		}

		// merge column mappings with the destination schema
		var columnMappings = new List<MySqlBulkCopyColumnMapping>(ColumnMappings);
		var addDefaultMappings = columnMappings.Count == 0;
		using (var cmd = new MySqlCommand("select * from " + tableName + ";", m_connection, m_transaction))
		using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SchemaOnly, ioBehavior, cancellationToken).ConfigureAwait(false))
		{
			var schema = reader.GetColumnSchema();
			for (var i = 0; i < Math.Min(m_valuesEnumerator!.FieldCount, schema.Count); i++)
			{
				var destinationColumn = reader.GetName(i);
				if (schema[i].DataTypeName == "BIT")
				{
					AddColumnMapping(m_logger, columnMappings, addDefaultMappings, i, destinationColumn, $"@`\uE002\bcol{i}`", $"%COL% = CAST(%VAR% AS UNSIGNED)");
				}
				else if (schema[i].DataTypeName == "YEAR")
				{
					// the current code can't distinguish between 0 = 0000 and 0 = 2000
					throw new NotSupportedException("'YEAR' columns are not supported by MySqlBulkLoader.");
				}
				else
				{
					var type = schema[i].DataType;
					if (type == typeof(byte[]) || (type == typeof(Guid) && (m_connection.GuidFormat is MySqlGuidFormat.Binary16 or MySqlGuidFormat.LittleEndianBinary16 or MySqlGuidFormat.TimeSwapBinary16)))
					{
						AddColumnMapping(m_logger, columnMappings, addDefaultMappings, i, destinationColumn, $"@`\uE002\bcol{i}`", $"%COL% = UNHEX(%VAR%)");
					}
					else if (addDefaultMappings)
					{
						Log.AddingDefaultColumnMapping(m_logger, i, destinationColumn);
						columnMappings.Add(new(i, destinationColumn));
					}
				}
			}
		}

		// set columns and expressions from the column mappings
		for (var i = 0; i < m_valuesEnumerator.FieldCount; i++)
		{
			var columnMapping = columnMappings.FirstOrDefault(x => x.SourceOrdinal == i);
			if (columnMapping is null)
			{
				Log.IgnoringColumn(m_logger, i);
				bulkLoader.Columns.Add("@`\uE002\bignore`");
			}
			else
			{
				if (columnMapping.DestinationColumn.Length == 0)
					throw new InvalidOperationException($"MySqlBulkCopyColumnMapping.DestinationName is not set for SourceOrdinal {columnMapping.SourceOrdinal}");
				if (columnMapping.DestinationColumn[0] == '@')
					bulkLoader.Columns.Add(columnMapping.DestinationColumn);
				else
					bulkLoader.Columns.Add(QuoteIdentifier(columnMapping.DestinationColumn));
				if (columnMapping.Expression is not null)
					bulkLoader.Expressions.Add(columnMapping.Expression);
			}
		}

		foreach (var columnMapping in columnMappings)
		{
			if (columnMapping.SourceOrdinal < 0 || columnMapping.SourceOrdinal >= m_valuesEnumerator.FieldCount)
				throw new InvalidOperationException($"SourceOrdinal {columnMapping.SourceOrdinal} is an invalid value");
		}

		var errors = new List<MySqlError>();
		MySqlInfoMessageEventHandler infoMessageHandler = (s, e) => errors.AddRange(e.Errors);
		m_connection.InfoMessage += infoMessageHandler;

		int rowsInserted;
		try
		{
			rowsInserted = await bulkLoader.LoadAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			m_connection.InfoMessage -= infoMessageHandler;
		}

		if (closeConnection)
			m_connection.Close();

		Log.FinishedBulkCopy(m_logger, tableName);

		if (!m_wasAborted && rowsInserted != m_rowsCopied && ConflictOption is MySqlBulkLoaderConflictOption.None)
		{
			Log.BulkCopyFailed(m_logger, tableName, m_rowsCopied, rowsInserted);
			throw new MySqlException(MySqlErrorCode.BulkCopyFailed, $"{m_rowsCopied} row{(m_rowsCopied == 1 ? " was" : "s were")} copied to {tableName} but only {rowsInserted} {(rowsInserted == 1 ? "was" : "were")} inserted.");
		}

		return new(errors, rowsInserted);

		static string QuoteIdentifier(string identifier) => "`" + identifier.Replace("`", "``") + "`";

		static void AddColumnMapping(ILogger logger, List<MySqlBulkCopyColumnMapping> columnMappings, bool addDefaultMappings, int destinationOrdinal, string destinationColumn, string variableName, string expression)
		{
			expression = expression.Replace("%COL%", "`" + destinationColumn + "`").Replace("%VAR%", variableName);
			var columnMapping = columnMappings.FirstOrDefault(x => destinationColumn.Equals(x.DestinationColumn, StringComparison.OrdinalIgnoreCase));
			if (columnMapping is not null)
			{
				if (columnMapping.Expression is not null)
				{
					Log.ColumnMappingAlreadyHasExpression(logger, columnMapping.SourceOrdinal, destinationColumn, columnMapping.Expression);
				}
				else
				{
					Log.SettingExpressionToMapColumn(logger, columnMapping.SourceOrdinal, destinationColumn, expression);
					columnMappings.Remove(columnMapping);
					columnMappings.Add(new(columnMapping.SourceOrdinal, variableName, expression));
				}
			}
			else if (addDefaultMappings)
			{
				Log.AddingDefaultColumnMapping(logger, destinationOrdinal, destinationColumn);
				columnMappings.Add(new(destinationOrdinal, variableName, expression));
			}
		}
	}

	internal async Task SendDataReaderAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		const int maxLength = 1048575;
		var buffer = ArrayPool<byte>.Shared.Rent(maxLength + 1);
		var outputIndex = 0;

		// allocate a reusable MySqlRowsCopiedEventArgs if event notification is necessary
		m_rowsCopied = 0;
		MySqlRowsCopiedEventArgs? eventArgs = null;
		if (NotifyAfter > 0 && MySqlRowsCopied is not null)
			eventArgs = new();

		try
		{
			var values = new object[m_valuesEnumerator!.FieldCount];
			Encoder? utf8Encoder = null;
			while (true)
			{
				var hasMore = ioBehavior == IOBehavior.Asynchronous ?
					await m_valuesEnumerator.MoveNextAsync().ConfigureAwait(false) :
					m_valuesEnumerator.MoveNext();
				if (!hasMore)
					break;

				m_valuesEnumerator.GetValues(values);
				for (var valueIndex = 0; valueIndex < values.Length; valueIndex++)
				{
					if (valueIndex > 0)
						buffer[outputIndex++] = (byte) '\t';

					var inputIndex = 0;
					var bytesWritten = 0;
					while (outputIndex >= maxLength || !WriteValue(m_connection, values[valueIndex], ref inputIndex, ref utf8Encoder, buffer.AsSpan(0, maxLength)[outputIndex..], out bytesWritten))
					{
						var payload = new PayloadData(new ArraySegment<byte>(buffer, 0, outputIndex + bytesWritten));
						await m_connection.Session.SendReplyAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
						outputIndex = 0;
						bytesWritten = 0;
					}
					outputIndex += bytesWritten;
				}
				buffer[outputIndex++] = (byte) '\n';

				m_rowsCopied++;
				if (eventArgs is not null && m_rowsCopied % NotifyAfter == 0)
				{
					eventArgs.RowsCopied = m_rowsCopied;
					MySqlRowsCopied!(this, eventArgs);
					if (eventArgs.Abort)
						break;
				}
			}

			if (outputIndex != 0 && eventArgs?.Abort is not true)
			{
				var payload2 = new PayloadData(new ArraySegment<byte>(buffer, 0, outputIndex));
				await m_connection.Session.SendReplyAsync(payload2, ioBehavior, cancellationToken).ConfigureAwait(false);
			}
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer);
			m_wasAborted = eventArgs?.Abort is true;
		}

		static bool WriteValue(MySqlConnection connection, object value, ref int inputIndex, ref Encoder? utf8Encoder, Span<byte> output, out int bytesWritten)
		{
			if (output.Length == 0)
			{
				bytesWritten = 0;
				return false;
			}

			if (value is null || value == DBNull.Value)
			{
				ReadOnlySpan<byte> escapedNull = @"\N"u8; // a field value of \N is read as NULL for input
				if (output.Length < escapedNull.Length)
				{
					bytesWritten = 0;
					return false;
				}
				escapedNull.CopyTo(output);
				bytesWritten = escapedNull.Length;
				return true;
			}
			else if (value is string stringValue)
			{
				return WriteSubstring(stringValue, ref inputIndex, ref utf8Encoder, output, out bytesWritten);
			}
			else if (value is char charValue)
			{
				return WriteString(charValue.ToString(), ref utf8Encoder, output, out bytesWritten);
			}
			else if (value is byte byteValue)
			{
				return Utf8Formatter.TryFormat(byteValue, output, out bytesWritten);
			}
			else if (value is sbyte sbyteValue)
			{
				return Utf8Formatter.TryFormat(sbyteValue, output, out bytesWritten);
			}
			else if (value is short shortValue)
			{
				return Utf8Formatter.TryFormat(shortValue, output, out bytesWritten);
			}
			else if (value is ushort ushortValue)
			{
				return Utf8Formatter.TryFormat(ushortValue, output, out bytesWritten);
			}
			else if (value is int intValue)
			{
				return Utf8Formatter.TryFormat(intValue, output, out bytesWritten);
			}
			else if (value is uint uintValue)
			{
				return Utf8Formatter.TryFormat(uintValue, output, out bytesWritten);
			}
			else if (value is long longValue)
			{
				return Utf8Formatter.TryFormat(longValue, output, out bytesWritten);
			}
			else if (value is ulong ulongValue)
			{
				return Utf8Formatter.TryFormat(ulongValue, output, out bytesWritten);
			}
			else if (value is decimal decimalValue)
			{
				return Utf8Formatter.TryFormat(decimalValue, output, out bytesWritten);
			}
			else if (value is byte[] or ReadOnlyMemory<byte> or Memory<byte> or ArraySegment<byte> or MySqlGeometry)
			{
				var inputSpan = value switch
				{
					byte[] byteArray => byteArray.AsSpan(),
					ArraySegment<byte> arraySegment => arraySegment.AsSpan(),
					Memory<byte> memory => memory.Span,
					MySqlGeometry geometry => geometry.ValueSpan,
					_ => ((ReadOnlyMemory<byte>) value).Span,
				};

				return WriteBytes(inputSpan, ref inputIndex, output, out bytesWritten);
			}
			else if (value is bool boolValue)
			{
				if (output.Length < 1)
				{
					bytesWritten = 0;
					return false;
				}
				output[0] = boolValue ? (byte) '1' : (byte) '0';
				bytesWritten = 1;
				return true;
			}
			else if (value is float floatValue)
			{
				// NOTE: Utf8Formatter doesn't support "R"
				return WriteString(floatValue.ToString("R", CultureInfo.InvariantCulture), ref utf8Encoder, output, out bytesWritten);
			}
			else if (value is double doubleValue)
			{
				// NOTE: Utf8Formatter doesn't support "R"
				return WriteString(doubleValue.ToString("R", CultureInfo.InvariantCulture), ref utf8Encoder, output, out bytesWritten);
			}
			else if (value is MySqlDateTime mySqlDateTimeValue)
			{
				if (mySqlDateTimeValue.IsValidDateTime)
					return WriteString(mySqlDateTimeValue.GetDateTime().ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'ffffff", CultureInfo.InvariantCulture), ref utf8Encoder, output, out bytesWritten);
				else
					return WriteString("0000-00-00", ref utf8Encoder, output, out bytesWritten);
			}
			else if (value is DateTime dateTimeValue)
			{
				if (connection.DateTimeKind == DateTimeKind.Utc && dateTimeValue.Kind == DateTimeKind.Local)
					throw new MySqlException("DateTime.Kind must not be Local when DateTimeKind setting is Utc");
				else if (connection.DateTimeKind == DateTimeKind.Local && dateTimeValue.Kind == DateTimeKind.Utc)
					throw new MySqlException("DateTime.Kind must not be Utc when DateTimeKind setting is Local");

				return WriteString(dateTimeValue.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'ffffff", CultureInfo.InvariantCulture), ref utf8Encoder, output, out bytesWritten);
			}
			else if (value is DateTimeOffset dateTimeOffsetValue)
			{
				// store as UTC as it will be read as such when deserialized from a timespan column
				return WriteString(dateTimeOffsetValue.UtcDateTime.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'ffffff", CultureInfo.InvariantCulture), ref utf8Encoder, output, out bytesWritten);
			}
#if NET6_0_OR_GREATER
			else if (value is DateOnly dateOnlyValue)
			{
				return WriteString(dateOnlyValue.ToString("yyyy'-'MM'-'dd", CultureInfo.InvariantCulture), ref utf8Encoder, output, out bytesWritten);
			}
			else if (value is TimeOnly timeOnlyValue)
			{
				return WriteString(timeOnlyValue.ToString("HH':'mm':'ss'.'ffffff", CultureInfo.InvariantCulture), ref utf8Encoder, output, out bytesWritten);
			}
#endif
			else if (value is TimeSpan ts)
			{
				var isNegative = false;
				if (ts.Ticks < 0)
				{
					isNegative = true;
					ts = TimeSpan.FromTicks(-ts.Ticks);
				}
#if NET6_0_OR_GREATER
				var str = string.Create(CultureInfo.InvariantCulture, $"{(isNegative ? "-" : "")}{ts.Days * 24 + ts.Hours}:{ts:mm':'ss'.'ffffff}");
#else
				var str = FormattableString.Invariant($"{(isNegative ? "-" : "")}{ts.Days * 24 + ts.Hours}:{ts:mm':'ss'.'ffffff}");
#endif
				return WriteString(str, ref utf8Encoder, output, out bytesWritten);
			}
			else if (value is Guid guidValue)
			{
				if (connection.GuidFormat is MySqlGuidFormat.Binary16 or MySqlGuidFormat.TimeSwapBinary16 or MySqlGuidFormat.LittleEndianBinary16)
				{
					var bytes = guidValue.ToByteArray();
					if (connection.GuidFormat != MySqlGuidFormat.LittleEndianBinary16)
					{
						Utility.SwapBytes(bytes, 0, 3);
						Utility.SwapBytes(bytes, 1, 2);
						Utility.SwapBytes(bytes, 4, 5);
						Utility.SwapBytes(bytes, 6, 7);

						if (connection.GuidFormat == MySqlGuidFormat.TimeSwapBinary16)
						{
							Utility.SwapBytes(bytes, 0, 4);
							Utility.SwapBytes(bytes, 1, 5);
							Utility.SwapBytes(bytes, 2, 6);
							Utility.SwapBytes(bytes, 3, 7);
							Utility.SwapBytes(bytes, 0, 2);
							Utility.SwapBytes(bytes, 1, 3);
						}
					}
					return WriteBytes(bytes, ref inputIndex, output, out bytesWritten);
				}
				else
				{
					var is32Characters = connection.GuidFormat == MySqlGuidFormat.Char32;
					return Utf8Formatter.TryFormat(guidValue, output, out bytesWritten, is32Characters ? 'N' : 'D');
				}
			}
			else if (value is Enum enumValue)
			{
				return WriteString(enumValue.ToString("d"), ref utf8Encoder, output, out bytesWritten);
			}
			else if (value is BigInteger bigInteger)
			{
				return WriteString(bigInteger.ToString(CultureInfo.InvariantCulture), ref utf8Encoder, output, out bytesWritten);
			}
			else if (value is MySqlDecimal mySqlDecimal)
			{
				return WriteString(mySqlDecimal.ToString(), ref utf8Encoder, output, out bytesWritten);
			}
			else
			{
				throw new NotSupportedException($"Type {value.GetType().Name} not currently supported. Value: {value}");
			}
		}

		static bool WriteString(string value, ref Encoder? utf8Encoder, Span<byte> output, out int bytesWritten)
		{
			var inputIndex = 0;
			if (WriteSubstring(value, ref inputIndex, ref utf8Encoder, output, out bytesWritten))
				return true;
			bytesWritten = 0;
			return false;
		}

		// Writes as much of 'value' as possible, starting at 'inputIndex' and writing UTF-8-encoded bytes to 'output'.
		// 'inputIndex' will be updated to the next character to be written, and 'bytesWritten' the number of bytes written to 'output'.
		static bool WriteSubstring(string value, ref int inputIndex, ref Encoder? utf8Encoder, Span<byte> output, out int bytesWritten)
		{
			bytesWritten = 0;
			while (inputIndex < value.Length)
			{
				if (Array.IndexOf(s_specialCharacters, value[inputIndex]) != -1)
				{
					if (output.Length <= 2)
						return false;

					output[0] = (byte) '\\';
					output[1] = (byte) value[inputIndex];
					output = output[2..];
					bytesWritten += 2;
					inputIndex++;
				}
				else
				{
					var nextIndex = value.IndexOfAny(s_specialCharacters, inputIndex);
					if (nextIndex == -1)
						nextIndex = value.Length;

					utf8Encoder ??= s_utf8Encoding.GetEncoder();
					if (output.Length < 4 && utf8Encoder.GetByteCount(value.AsSpan(inputIndex, Math.Min(2, nextIndex - inputIndex)), flush: false) > output.Length)
						return false;
					utf8Encoder.Convert(value.AsSpan(inputIndex, nextIndex - inputIndex), output, nextIndex == value.Length, out var charsUsed, out var bytesUsed, out var completed);

					bytesWritten += bytesUsed;
					output = output[bytesUsed..];
					inputIndex += charsUsed;

					if (!completed)
						return false;
				}
			}

			return true;
		}

		static bool WriteBytes(ReadOnlySpan<byte> value, ref int inputIndex, Span<byte> output, out int bytesWritten)
		{
			ReadOnlySpan<byte> hex = "0123456789ABCDEF"u8;
			bytesWritten = 0;
			for (; inputIndex < value.Length && output.Length > 2; inputIndex++)
			{
				var by = value[inputIndex];
				output[0] = hex[(by >> 4) & 0xF];
				output[1] = hex[by & 0xF];
				output = output[2..];
				bytesWritten += 2;
			}

			return inputIndex == value.Length;
		}
	}

	private static readonly char[] s_specialCharacters = new char[] { '\t', '\\', '\n' };
	private static readonly UTF8Encoding s_utf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

	private readonly MySqlConnection m_connection;
	private readonly MySqlTransaction? m_transaction;
	private readonly ILogger m_logger;
	private int m_rowsCopied;
	private IValuesEnumerator? m_valuesEnumerator;
	private bool m_wasAborted;
}
