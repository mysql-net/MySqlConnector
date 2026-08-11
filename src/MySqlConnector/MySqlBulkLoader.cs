using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector;

/// <summary>
/// <para><see cref="MySqlBulkLoader"/> lets you efficiently load a MySQL Server Table with data from a CSV or TSV file or <see cref="Stream"/>.</para>
/// <para>Example code:</para>
/// <code>
/// await using var connection = new MySqlConnection("...;AllowLoadLocalInfile=True");
/// await connection.OpenAsync();
/// var bulkLoader = new MySqlBulkLoader(connection)
/// {
/// 	FileName = @"C:\Path\To\file.csv",
/// 	TableName = "destination",
/// 	CharacterSet = "UTF8",
/// 	NumberOfLinesToSkip = 1,
/// 	FieldTerminator = ",",
/// 	FieldQuotationCharacter = '"',
/// 	FieldQuotationOptional = true,
/// 	Local = true,
/// }
/// var rowCount = await bulkLoader.LoadAsync();
/// </code>
/// </summary>
/// <remarks>Due to <a href="https://mysqlconnector.net/troubleshooting/load-data-local-infile/">security features</a>
/// in MySQL Server, the connection string <strong>must</strong> have <c>AllowLoadLocalInfile=true</c> in order to use a local source.
/// </remarks>
public sealed class MySqlBulkLoader
{
	/// <summary>
	/// (Optional) The character set of the source data. By default, the database's character set is used.
	/// </summary>
	public string? CharacterSet { get; set; }

	/// <summary>
	/// (Optional) A list of the column names in the destination table that should be filled with data from the input file.
	/// </summary>
	public List<string> Columns { get; }

	/// <summary>
	/// A <see cref="MySqlBulkLoaderConflictOption"/> value that specifies how conflicts are resolved (default <see cref="MySqlBulkLoaderConflictOption.None"/>).
	/// </summary>
	public MySqlBulkLoaderConflictOption ConflictOption { get; set; }

	/// <summary>
	/// The <see cref="MySqlConnection"/> to use.
	/// </summary>
	public MySqlConnection Connection { get; set; }

	/// <summary>
	/// (Optional) The character used to escape instances of <see cref="FieldQuotationCharacter"/> within field values.
	/// </summary>
	public char EscapeCharacter { get; set; }

	/// <summary>
	/// (Optional) A list of expressions used to set field values from the columns in the source data.
	/// </summary>
	public List<string> Expressions { get; }

	/// <summary>
	/// (Optional) The character used to enclose fields in the source data.
	/// </summary>
	public char FieldQuotationCharacter { get; set; }

	/// <summary>
	/// Whether quoting fields is optional (default <c>false</c>).
	/// </summary>
	public bool FieldQuotationOptional { get; set; }

	/// <summary>
	/// (Optional) The string fields are terminated with.
	/// </summary>
	public string? FieldTerminator { get; set; }

	/// <summary>
	/// The name of the local (if <see cref="Local"/> is <c>true</c>) or remote (otherwise) file to load.
	/// Either this or <see cref="SourceStream"/> must be set.
	/// </summary>
	public string? FileName { get; set; }

	/// <summary>
	/// (Optional) A prefix in each line that should be skipped when loading.
	/// </summary>
	public string? LinePrefix { get; set; }

	/// <summary>
	/// (Optional) The string lines are terminated with.
	/// </summary>
	public string? LineTerminator { get; set; }

	/// <summary>
	/// Whether a local file is being used (default <c>true</c>).
	/// </summary>
	public bool Local { get; set; }

	/// <summary>
	/// The number of lines to skip at the beginning of the file (default <c>0</c>).
	/// </summary>
	public int NumberOfLinesToSkip { get; set; }

	/// <summary>
	/// A <see cref="MySqlBulkLoaderPriority"/> giving the priority to load with (default <see cref="MySqlBulkLoaderPriority.None"/>).
	/// </summary>
	public MySqlBulkLoaderPriority Priority { get; set; }

	/// <summary>
	/// A <see cref="Stream"/> containing the data to load. Either this or <see cref="FileName"/> must be set.
	/// The <see cref="Local"/> property must be <c>true</c> if this is set.
	/// </summary>
	public Stream? SourceStream
	{
		get => Source as Stream;
		set => Source = value;
	}

	/// <summary>
	/// The name of the table to load into. If this is a reserved word or contains spaces, it must be quoted.
	/// </summary>
	public string? TableName { get; set; }

	/// <summary>
	/// The timeout (in milliseconds) to use.
	/// </summary>
	public int Timeout { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="MySqlBulkLoader"/> class with the specified <see cref="MySqlConnection"/>.
	/// </summary>
	/// <param name="connection">The <see cref="MySqlConnection"/> to use.</param>
	public MySqlBulkLoader(MySqlConnection connection)
	{
		Connection = connection;
		Local = true;
		Columns = [];
		Expressions = [];
	}

	/// <summary>
	/// Loads all data in the source file or stream into the destination table.
	/// </summary>
	/// <returns>The number of rows inserted.</returns>
#pragma warning disable CA2012 // Safe because method completes synchronously
	public int Load() => LoadAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore CA2012

	/// <summary>
	/// Asynchronously loads all data in the source file or stream into the destination table.
	/// </summary>
	/// <returns>A <see cref="Task{Int32}"/> that will be completed with the number of rows inserted.</returns>
	public Task<int> LoadAsync() => LoadAsync(IOBehavior.Asynchronous, CancellationToken.None).AsTask();

	/// <summary>
	/// Asynchronously loads all data in the source file or stream into the destination table.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>A <see cref="Task{Int32}"/> that will be completed with the number of rows inserted.</returns>
	public Task<int> LoadAsync(CancellationToken cancellationToken) => LoadAsync(IOBehavior.Asynchronous, cancellationToken).AsTask();

	internal async ValueTask<int> LoadAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		if (Connection is null)
			throw new InvalidOperationException("Connection not set");

		if (string.IsNullOrWhiteSpace(TableName))
			throw new InvalidOperationException("TableName is required.");

		if (!string.IsNullOrWhiteSpace(FileName) && Source is not null)
			throw new InvalidOperationException("Exactly one of FileName or SourceStream must be set.");

		if (!string.IsNullOrWhiteSpace(FileName))
		{
			if (Local)
			{
				// replace the file name with a sentinel so that we know (when processing the result set) that it's not spoofed by the server
				var newFileName = GenerateSourceFileName();
				AddSource(newFileName, CreateFileStream(FileName!));
				FileName = newFileName;
			}
		}
		else
		{
			if (!Local)
				throw new InvalidOperationException("Local must be true to use SourceStream.");

			FileName = GenerateSourceFileName();
			AddSource(FileName, Source!);
		}

		var closeConnection = false;
		if (Connection.State != ConnectionState.Open)
		{
			closeConnection = true;
			Connection.Open();
		}

		bool closeStream = SourceStream is not null;
		try
		{
			if (Local && !Connection.AllowLoadLocalInfile)
				throw new NotSupportedException("To use MySqlBulkLoader.Local=true, set AllowLoadLocalInfile=true in the connection string. See https://mysqlconnector.net/load-data");

			using var cmd = new MySqlCommand(CreateSql(), Connection, Connection.CurrentTransaction)
			{
				AllowUserVariables = true,
				CommandTimeout = Timeout,
			};
			var result = await cmd.ExecuteNonQueryAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			closeStream = false;
			return result;
		}
		finally
		{
			if (closeStream && TryGetAndRemoveSource(FileName!, out var source))
				((IDisposable) source).Dispose();

			if (closeConnection)
				Connection.Close();
		}

		static void AddSource(string name, object source)
		{
			lock (s_lock)
				s_sources.Add(name, source);
		}
	}

	internal const string SourcePrefix = ":SOURCE:";

	internal object? Source { get; set; }

	private string CreateSql()
	{
		var sb = new StringBuilder("LOAD DATA ");

		sb.Append(Priority switch
		{
			MySqlBulkLoaderPriority.Low => "LOW_PRIORITY ",
			MySqlBulkLoaderPriority.Concurrent => "CONCURRENT ",
			_ => "",
		});

		if (Local)
			sb.Append("LOCAL ");

#pragma warning disable CA1305 // StringBuilder.Append is only being used with strings, which aren't locale-sensitive
		sb.Append($"INFILE '{MySqlHelper.EscapeString(FileName!)}' ");

		sb.Append(ConflictOption switch
		{
			MySqlBulkLoaderConflictOption.Replace => "REPLACE ",
			MySqlBulkLoaderConflictOption.Ignore => "IGNORE ",
			_ => "",
		});

		sb.Append($"INTO TABLE {TableName} ");

		if (CharacterSet is not null)
			sb.Append($"CHARACTER SET {CharacterSet} ");

		var fieldsTerminatedBy = FieldTerminator is null ? "" : $"TERMINATED BY '{MySqlHelper.EscapeString(FieldTerminator)}' ";
		var fieldsEnclosedBy = FieldQuotationCharacter == default ? "" : $"{(FieldQuotationOptional ? "OPTIONALLY " : "")}ENCLOSED BY '{MySqlHelper.EscapeString(FieldQuotationCharacter.ToString())}' ";
		var fieldsEscapedBy = EscapeCharacter == default ? "" : $"ESCAPED BY '{MySqlHelper.EscapeString(EscapeCharacter.ToString())}' ";
		if (fieldsTerminatedBy.Length + fieldsEnclosedBy.Length + fieldsEscapedBy.Length > 0)
			sb.Append($"FIELDS {fieldsTerminatedBy}{fieldsEnclosedBy}{fieldsEscapedBy}");

		var linesTerminatedBy = LineTerminator is null ? "" : $"TERMINATED BY '{MySqlHelper.EscapeString(LineTerminator)}' ";
		var linesStartingBy = LinePrefix is null ? "" : $"STARTING BY '{MySqlHelper.EscapeString(LinePrefix)}' ";
		if (linesTerminatedBy.Length + linesStartingBy.Length > 0)
			sb.Append($"LINES {linesTerminatedBy}{linesStartingBy}");

		sb.AppendFormat(CultureInfo.InvariantCulture, "IGNORE {0} LINES ", NumberOfLinesToSkip);

		if (Columns.Count > 0)
			sb.Append($"({string.Join(",", Columns)}) ");

		if (Expressions.Count > 0)
			sb.Append($"SET {string.Join(",", Expressions)}");

		sb.Append(';');
#pragma warning restore CA1305

		return sb.ToString();
	}

	private static FileStream CreateFileStream(string fileName)
	{
		try
		{
			return File.OpenRead(fileName);
		}
		catch (Exception ex)
		{
			throw new MySqlException($"Could not access file \"{fileName}\"", ex);
		}
	}

	internal static object GetAndRemoveSource(string sourceKey)
	{
		lock (s_lock)
		{
			var source = s_sources[sourceKey];
			_ = s_sources.Remove(sourceKey);
			return source;
		}
	}

	internal static bool TryGetAndRemoveSource(string sourceKey, [NotNullWhen(true)] out object? source)
	{
		lock (s_lock)
		{
			if (s_sources.TryGetValue(sourceKey, out source))
				return s_sources.Remove(sourceKey);
		}

		return false;
	}

	private static string GenerateSourceFileName() => SourcePrefix + Guid.NewGuid().ToString("N");

#if NET9_0_OR_GREATER
	private static readonly Lock s_lock = new();
#else
	private static readonly object s_lock = new();
#endif
	private static readonly Dictionary<string, object> s_sources = [];
}
