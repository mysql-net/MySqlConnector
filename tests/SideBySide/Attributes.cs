using System;
using MySql.Data.MySqlClient;
using Xunit;

namespace SideBySide
{
	public class RequiresFeatureFactAttribute : FactAttribute
	{
		public RequiresFeatureFactAttribute()
		{
		}

		public RequiresFeatureFactAttribute(ServerFeatures features)
		{
			if (!AppConfig.SupportedFeatures.HasFlag(features))
				Skip = "Doesn't support " + features;
		}

		public bool RequiresSsl
		{
			get => m_requiresSsl;
			set
			{
				m_requiresSsl = value;
				if (m_requiresSsl)
				{
					var csb = AppConfig.CreateConnectionStringBuilder();
					if (csb.SslMode == MySqlSslMode.None || csb.SslMode == MySqlSslMode.Preferred)
						Skip = "SSL not explicitly required";
				}
			}
		}

		private bool m_requiresSsl;
	}

	public class RequiresFeatureTheoryAttribute : TheoryAttribute
	{
		public RequiresFeatureTheoryAttribute()
		{
		}

		public RequiresFeatureTheoryAttribute(ServerFeatures features)
		{
			if (!AppConfig.SupportedFeatures.HasFlag(features))
				Skip = "Doesn't support " + features;
		}

		public bool RequiresSsl
		{
			get => m_requiresSsl;
			set
			{
				m_requiresSsl = value;
				if (m_requiresSsl)
				{
					var csb = AppConfig.CreateConnectionStringBuilder();
					if (csb.SslMode == MySqlSslMode.None || csb.SslMode == MySqlSslMode.Preferred)
						Skip = "SSL not explicitly required";
				}
			}
		}

		private bool m_requiresSsl;
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

		public bool TrustedHost
		{
			
			get => _trustedHost;
			set
			{
				_trustedHost = value;

				var csb = AppConfig.CreateConnectionStringBuilder();
				if (_trustedHost)
				{
					if (csb.SslMode == MySqlSslMode.None
						|| csb.SslMode == MySqlSslMode.Preferred
						|| csb.SslMode == MySqlSslMode.Required)
						Skip = "SslMode should be VerifyCA or higher.";
				}
				else
				{
					if (csb.SslMode == MySqlSslMode.VerifyCA
						|| csb.SslMode == MySqlSslMode.VerifyFull)
						Skip = "SslMode should be less than VerifyCA.";
				}
			}
		}
		private bool _trustedHost;
	}

	public class BulkLoaderLocalTsvFileFactAttribute : FactAttribute
	{
		public BulkLoaderLocalTsvFileFactAttribute()
		{
			if(string.IsNullOrWhiteSpace(AppConfig.MySqlBulkLoaderLocalTsvFile))
				Skip = "No bulk loader local TSV file specified";
		}
	}

	public class UnbufferedResultSetsFactAttribute : FactAttribute
	{
#if !BASELINE
		public UnbufferedResultSetsFactAttribute()
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			if(csb.BufferResultSets == true)
				Skip = "Do not run when BufferResultSets are used";
		}
#endif
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
}
