using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlBulkLoader
	{
		public string? CharacterSet { get; set; }
		public List<string> Columns { get; }
		public MySqlBulkLoaderConflictOption ConflictOption { get; set; }
		public MySqlConnection Connection { get; set; }
		public char EscapeCharacter { get; set; }
		public List<string> Expressions { get; }
		public char FieldQuotationCharacter { get; set; }
		public bool FieldQuotationOptional { get; set; }
		public string? FieldTerminator { get; set; }

		/// <summary>
		/// The name of the local (if <see cref="Local"/> is <c>true</c>) or remote (otherwise) file to load.
		/// Either this or <see cref="SourceStream"/> must be set.
		/// </summary>
		public string? FileName { get; set; }

		public string? LinePrefix { get; set; }
		public string? LineTerminator { get; set; }
		public bool Local { get; set; }
		public int NumberOfLinesToSkip { get; set; }
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

		public string? TableName { get; set; }
		public int Timeout { get; set; }

		public MySqlBulkLoader(MySqlConnection connection)
		{
			Connection = connection;
			Local = true;
			Columns = new List<string>();
			Expressions = new List<string>();
		}

		public int Load() => LoadAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();

		public Task<int> LoadAsync() => LoadAsync(IOBehavior.Asynchronous, CancellationToken.None).AsTask();

		public Task<int> LoadAsync(CancellationToken cancellationToken) => LoadAsync(IOBehavior.Asynchronous, cancellationToken).AsTask();

		internal async ValueTask<int> LoadAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			if (Connection is null)
				throw new InvalidOperationException("Connection not set");

			if (string.IsNullOrWhiteSpace(TableName))
				throw new InvalidOperationException("TableName is required.");

			if (!string.IsNullOrWhiteSpace(FileName) && Source is object)
				throw new InvalidOperationException("Exactly one of FileName or SourceStream must be set.");

			if (!string.IsNullOrWhiteSpace(FileName))
			{
				if (Local)
				{
					// replace the file name with a sentinel so that we know (when processing the result set) that it's not spoofed by the server
					var newFileName = GenerateSourceFileName();
					lock (s_lock)
						s_sources.Add(newFileName, CreateFileStream(FileName!));
					FileName = newFileName;
				}
			}
			else
			{
				if (!Local)
					throw new InvalidOperationException("Local must be true to use SourceStream, SourceDataTable, or SourceDataReader.");

				FileName = GenerateSourceFileName();
				lock (s_lock)
					s_sources.Add(FileName, Source!);
			}

			var closeConnection = false;
			if (Connection.State != ConnectionState.Open)
			{
				closeConnection = true;
				Connection.Open();
			}

			bool closeStream = SourceStream is object;
			try
			{
				if (Local && !Connection.AllowLoadLocalInfile)
					throw new NotSupportedException("To use MySqlBulkLoader.Local=true, set AllowLoadLocalInfile=true in the connection string. See https://fl.vu/mysql-load-data");

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
		}

		internal const string SourcePrefix = ":SOURCE:";

		internal object? Source { get; set; }

		private string CreateSql()
		{
			var sb = new StringBuilder("LOAD DATA ");

			sb.Append(Priority switch
			{
				MySqlBulkLoaderPriority.Low => "LOW_PRIORITY ",
				MySqlBulkLoaderPriority.Concurrent => "LOCAL ",
				_ => "",
			});

			if (Local)
				sb.Append("LOCAL ");

			sb.AppendFormat(CultureInfo.InvariantCulture, "INFILE '{0}' ", MySqlHelper.EscapeString(FileName!));

			sb.Append(ConflictOption switch
			{
				MySqlBulkLoaderConflictOption.Replace => "REPLACE ",
				MySqlBulkLoaderConflictOption.Ignore => "IGNORE ",
				_ => "",
			});

			sb.AppendFormat(CultureInfo.InvariantCulture, "INTO TABLE {0} ", TableName);

			if (CharacterSet is object)
				sb.AppendFormat(CultureInfo.InvariantCulture, "CHARACTER SET {0} ", CharacterSet);

			var fieldsTerminatedBy = FieldTerminator is null ? "" : "TERMINATED BY '{0}' ".FormatInvariant(MySqlHelper.EscapeString(FieldTerminator));
			var fieldsEnclosedBy = FieldQuotationCharacter == default ? "" : "{0}ENCLOSED BY '{1}' ".FormatInvariant(FieldQuotationOptional ? "OPTIONALLY " : "", MySqlHelper.EscapeString(FieldQuotationCharacter.ToString()));
			var fieldsEscapedBy = EscapeCharacter == default ? "" : "ESCAPED BY '{0}' ".FormatInvariant(MySqlHelper.EscapeString(EscapeCharacter.ToString()));
			if (fieldsTerminatedBy.Length + fieldsEnclosedBy.Length + fieldsEscapedBy.Length > 0)
				sb.AppendFormat(CultureInfo.InvariantCulture, "FIELDS {0}{1}{2}", fieldsTerminatedBy, fieldsEnclosedBy, fieldsEscapedBy);

			var linesTerminatedBy = LineTerminator is null ? "" : "TERMINATED BY '{0}' ".FormatInvariant(MySqlHelper.EscapeString(LineTerminator));
			var linesStartingBy = LinePrefix is null ? "" : "STARTING BY '{0}' ".FormatInvariant(MySqlHelper.EscapeString(LinePrefix));
			if (linesTerminatedBy.Length + linesStartingBy.Length > 0)
				sb.AppendFormat(CultureInfo.InvariantCulture, "LINES {0}{1}", linesTerminatedBy, linesStartingBy);

			sb.AppendFormat(CultureInfo.InvariantCulture, "IGNORE {0} LINES ", NumberOfLinesToSkip);

			if (Columns.Count > 0)
				sb.AppendFormat(CultureInfo.InvariantCulture, "({0}) ", string.Join(",", Columns));

			if (Expressions.Count > 0)
				sb.AppendFormat("SET {0}", string.Join(",", Expressions));

			sb.Append(';');

			return sb.ToString();
		}

		private Stream CreateFileStream(string fileName)
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
				s_sources.Remove(sourceKey);
				return source;
			}
		}

		internal static bool TryGetAndRemoveSource(string sourceKey, [NotNullWhen(true)] out object? source)
		{
			lock (s_lock)
			{
				if (s_sources.TryGetValue(sourceKey, out source))
				{
					s_sources.Remove(sourceKey);
					return true;
				}
			}

			return false;
		}

		private static string GenerateSourceFileName() => SourcePrefix + Guid.NewGuid().ToString("N");

		static readonly object s_lock = new object();
		static readonly Dictionary<string, object> s_sources = new Dictionary<string, object>();
	}
}
