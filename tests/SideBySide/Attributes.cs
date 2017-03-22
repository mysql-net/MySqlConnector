using System;
using MySql.Data.MySqlClient;
using Xunit;

namespace SideBySide
{
	public class CachedProcedureTheoryAttribute : TheoryAttribute
	{
		public CachedProcedureTheoryAttribute()
		{
			if(!AppConfig.SupportsCachedProcedures)
				Skip = "No Cached Procedure Support";
		}
	}

	public class JsonTheoryAttribute : TheoryAttribute
	{
		public JsonTheoryAttribute()
		{
			if(!AppConfig.SupportsJson)
				Skip = "No JSON Support";
		}
	}

	public class PasswordlessUserFactAttribute : FactAttribute
	{
		public PasswordlessUserFactAttribute()
		{
			if(string.IsNullOrWhiteSpace(AppConfig.PasswordlessUser))
				Skip = "No passwordless user";
		}
	}

	public class BulkLoaderCsvFileFactAttribute : FactAttribute
	{
		public BulkLoaderCsvFileFactAttribute()
		{
			if(string.IsNullOrWhiteSpace(AppConfig.MySqlBulkLoaderCsvFile))
				Skip = "No bulk loader CSV file specified";
		}
	}

	public class BulkLoaderTsvFileFactAttribute : FactAttribute
	{
		public BulkLoaderTsvFileFactAttribute()
		{
			if(string.IsNullOrWhiteSpace(AppConfig.MySqlBulkLoaderTsvFile))
				Skip = "No bulk loader TSV file specified";
		}
	}

	public class BulkLoaderLocalCsvFileFactAttribute : FactAttribute
	{
		public BulkLoaderLocalCsvFileFactAttribute()
		{
			if(string.IsNullOrWhiteSpace(AppConfig.MySqlBulkLoaderLocalCsvFile))
				Skip = "No bulk loader local CSV file specified";
		}
	}

	public class BulkLoaderLocalTsvFileFactAttribute : FactAttribute
	{
		public BulkLoaderLocalTsvFileFactAttribute()
		{
			if(string.IsNullOrWhiteSpace(AppConfig.MySqlBulkLoaderLocalTsvFile))
				Skip = "No bulk loader local TSV file specified";
		}
	}

	public class TcpConnectionFactAttribute : FactAttribute
	{
		public TcpConnectionFactAttribute()
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			if(csb.Server.StartsWith("/", StringComparison.Ordinal) || csb.Server.StartsWith("./", StringComparison.Ordinal))
				Skip = "Not a TCP Connection";
		}
	}

	public class SecondaryDatabaseRequiredFactAttribute : FactAttribute
	{
		public SecondaryDatabaseRequiredFactAttribute()
		{
			if (string.IsNullOrEmpty(AppConfig.SecondaryDatabase))
				Skip = "No SecondaryDatabase specified.";
		}
	}

	public class SslRequiredConnectionFactAttribute : FactAttribute
	{
		public SslRequiredConnectionFactAttribute()
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			if(csb.SslMode == MySqlSslMode.None || csb.SslMode == MySqlSslMode.Preferred)
				Skip = "SSL not explicitly required";
		}
	}
}
