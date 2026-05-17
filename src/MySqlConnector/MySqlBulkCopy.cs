using System.Buffers;
using System.Text;
using Microsoft.Extensions.Logging;
using MySqlConnector.Core;
using MySqlConnector.Helpers;
using MySqlConnector.Logging;
using MySqlConnector.Protocol;
using MySqlConnector.Protocol.Serialization;

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
/// await using var connection = new MySqlConnection("...;AllowLoadLocalInfile=True");
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
		ArgumentNullException.ThrowIfNull(connection);
		m_connection = connection;
		m_transaction = transaction;
		m_logger = m_connection.LoggingConfiguration.BulkCopyLogger;
		ColumnMappings = [];
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
		ArgumentNullException.ThrowIfNull(dataTable);
		m_valuesEnumerator = DataRowsValuesEnumerator.Create(dataTable);
#pragma warning disable CA2012 // Safe because method completes synchronously
		return WriteToServerAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore CA2012
	}

	/// <summary>
	/// Asynchronously copies all rows in the supplied <see cref="DataTable"/> to the destination table specified by the
	/// <see cref="DestinationTableName"/> property of the <see cref="MySqlBulkCopy"/> object.
	/// </summary>
	/// <param name="dataTable">The <see cref="DataTable"/> to copy.</param>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>A <see cref="MySqlBulkCopyResult"/> with the result of the bulk copy operation.</returns>
	public async ValueTask<MySqlBulkCopyResult> WriteToServerAsync(DataTable dataTable, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(dataTable);
		m_valuesEnumerator = DataRowsValuesEnumerator.Create(dataTable);
		return await WriteToServerAsync(IOBehavior.Asynchronous, cancellationToken).ConfigureAwait(false);
	}

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
		ArgumentNullException.ThrowIfNull(dataRows);
		m_valuesEnumerator = new DataRowsValuesEnumerator(dataRows, columnCount);
#pragma warning disable CA2012 // Safe because method completes synchronously
		return WriteToServerAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore CA2012
	}

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
		ArgumentNullException.ThrowIfNull(dataRows);
		m_valuesEnumerator = new DataRowsValuesEnumerator(dataRows, columnCount);
		return await WriteToServerAsync(IOBehavior.Asynchronous, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Copies all rows in the supplied <see cref="IDataReader"/> to the destination table specified by the
	/// <see cref="DestinationTableName"/> property of the <see cref="MySqlBulkCopy"/> object.
	/// </summary>
	/// <param name="dataReader">The <see cref="IDataReader"/> to copy from.</param>
	/// <returns>A <see cref="MySqlBulkCopyResult"/> with the result of the bulk copy operation.</returns>
	public MySqlBulkCopyResult WriteToServer(IDataReader dataReader)
	{
		ArgumentNullException.ThrowIfNull(dataReader);
		m_valuesEnumerator = DataReaderValuesEnumerator.Create(dataReader);
#pragma warning disable CA2012 // Safe because method completes synchronously
		return WriteToServerAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore CA2012
	}

	/// <summary>
	/// Asynchronously copies all rows in the supplied <see cref="IDataReader"/> to the destination table specified by the
	/// <see cref="DestinationTableName"/> property of the <see cref="MySqlBulkCopy"/> object.
	/// </summary>
	/// <param name="dataReader">The <see cref="IDataReader"/> to copy from.</param>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>A <see cref="MySqlBulkCopyResult"/> with the result of the bulk copy operation.</returns>
	public async ValueTask<MySqlBulkCopyResult> WriteToServerAsync(IDataReader dataReader, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(dataReader);
		m_valuesEnumerator = DataReaderValuesEnumerator.Create(dataReader);
		return await WriteToServerAsync(IOBehavior.Asynchronous, cancellationToken).ConfigureAwait(false);
	}

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
			for (var i = 0; i < schema.Count; i++)
			{
				var destinationColumn = reader.GetName(i);
				var dataTypeName = schema[i].DataTypeName;
				if (dataTypeName == "BIT")
				{
					AddColumnMapping(m_logger, columnMappings, addDefaultMappings, i, destinationColumn, $"@`\uE002\bcol{i}`", $"%COL% = CAST(%VAR% AS UNSIGNED)");
				}
				else
				{
					var type = schema[i].DataType;
					if (type == typeof(byte[]) ||
						dataTypeName == "VECTOR" ||
						(type == typeof(Guid) && (m_connection.GuidFormat is MySqlGuidFormat.Binary16 or MySqlGuidFormat.LittleEndianBinary16 or MySqlGuidFormat.TimeSwapBinary16)))
					{
						AddColumnMapping(m_logger, columnMappings, addDefaultMappings, i, destinationColumn, $"@`\uE002\bcol{i}`", $"%COL% = UNHEX(%VAR%)");
					}
					else if (addDefaultMappings)
					{
						if (schema[i].DataTypeName == "YEAR")
						{
							// the current code can't distinguish between 0 = 0000 and 0 = 2000
							throw new NotSupportedException("'YEAR' columns are not supported by MySqlBulkCopy.");
						}

						Log.AddingDefaultColumnMapping(m_logger, i, destinationColumn);
						columnMappings.Add(new(i, destinationColumn));
					}
				}
			}
		}

		// set columns and expressions from the column mappings
		for (var i = 0; i < m_valuesEnumerator!.FieldCount; i++)
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
				if (columnMapping.DestinationColumn[0] == '@' && columnMapping.Expression is not null)
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
			const byte tabByte = (byte) '\t';
			const byte newLineByte = (byte) '\n';

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
						buffer[outputIndex++] = tabByte;

					var inputIndex = 0;
					var bytesWritten = 0;
					while (outputIndex >= maxLength || !ValueWriteHelper.WriteValue(m_connection, values[valueIndex], ref inputIndex, ref utf8Encoder, buffer.AsSpan(0, maxLength)[outputIndex..], out bytesWritten))
					{
						var payload = new PayloadData(new ArraySegment<byte>(buffer, 0, outputIndex + bytesWritten));
						await m_connection.Session.SendReplyAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
						outputIndex = 0;
						bytesWritten = 0;
					}
					outputIndex += bytesWritten;
				}
				buffer[outputIndex++] = newLineByte;

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
	}

	private readonly MySqlConnection m_connection;
	private readonly MySqlTransaction? m_transaction;
	private readonly ILogger m_logger;
	private int m_rowsCopied;
	private IValuesEnumerator? m_valuesEnumerator;
	private bool m_wasAborted;
}
