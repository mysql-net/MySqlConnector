using System;
using MySql.Data.MySqlClient;
using Xunit;

namespace MySql.Data.Tests
{
	public class MySqlConnectionStringBuilderTests
	{
		[Fact]
		public void Defaults()
		{
			var csb = new MySqlConnectionStringBuilder();
			Assert.Equal(false, csb.AllowUserVariables);
			Assert.Equal(null, csb.CertificateFile);
			Assert.Equal(null, csb.CertificatePassword);
			Assert.Equal("", csb.CharacterSet);
#if BASELINE
			Assert.Equal(false, csb.ConnectionReset);
#else
			Assert.Equal(true, csb.ConnectionReset);
#endif
			Assert.Equal(15u, csb.ConnectionTimeout);
			Assert.Equal(false, csb.ConvertZeroDateTime);
			Assert.Equal("", csb.Database);
#if !BASELINE
			Assert.Equal(false, csb.ForceSynchronous);
#endif
			Assert.Equal(0u, csb.Keepalive);
			Assert.Equal(100u, csb.MaximumPoolSize);
			Assert.Equal(0u, csb.MinimumPoolSize);
			Assert.Equal("", csb.Password);
			Assert.Equal(false, csb.OldGuids);
			Assert.Equal(false, csb.PersistSecurityInfo);
			Assert.Equal(true, csb.Pooling);
			Assert.Equal(3306u, csb.Port);
			Assert.Equal("", csb.Server);
#if BASELINE
			Assert.Equal(MySqlSslMode.Prefered, csb.SslMode);
#else
			Assert.Equal(MySqlSslMode.None, csb.SslMode);
#endif
			Assert.Equal(true, csb.TreatTinyAsBoolean);
			Assert.Equal(false, csb.UseCompression);
			Assert.Equal("", csb.UserID);
#if BASELINE
			Assert.False(csb.UseAffectedRows);
#else
			Assert.True(csb.UseAffectedRows);
#endif
		}

		[Fact]
		public void ParseConnectionString()
		{
			var csb = new MySqlConnectionStringBuilder
			{
				ConnectionString = "Data Source=db-server;" +
				                   "Initial Catalog=schema_name;" +
				                   "Allow User Variables=true;" +
				                   "certificate file=file.pfx;" +
				                   "certificate password=Pass1234;" +
				                   "Character Set=latin1;" +
				                   "Compress=true;" +
				                   "connect timeout=30;" +
				                   "ConnectionReset=false;" +
				                   "Convert Zero Datetime=true;" +
#if !BASELINE
				                   "forcesynchronous=true;" +
#endif
				                   "Keep Alive=90;" +
				                   "minpoolsize=5;" +
				                   "maxpoolsize=15;" +
				                   "OldGuids=true;" +
				                   "persistsecurityinfo=yes;" +
				                   "Pooling=no;" +
				                   "Port=1234;" +
				                   "pwd=Pass1234;" +
				                   "Treat Tiny As Boolean=false;" +
				                   "ssl mode=verifyca;" +
				                   "Uid=username;" +
				                   "useaffectedrows=false"
			};
			Assert.Equal(true, csb.AllowUserVariables);
			Assert.Equal("file.pfx", csb.CertificateFile);
			Assert.Equal("Pass1234", csb.CertificatePassword);
			Assert.Equal("latin1", csb.CharacterSet);
			Assert.Equal(false, csb.ConnectionReset);
			Assert.Equal(30u, csb.ConnectionTimeout);
			Assert.Equal(true, csb.ConvertZeroDateTime);
			Assert.Equal("schema_name", csb.Database);
#if !BASELINE
			Assert.Equal(true, csb.ForceSynchronous);
#endif
			Assert.Equal(90u, csb.Keepalive);
			Assert.Equal(15u, csb.MaximumPoolSize);
			Assert.Equal(5u, csb.MinimumPoolSize);
			Assert.Equal("Pass1234", csb.Password);
			Assert.Equal(true, csb.OldGuids);
			Assert.Equal(true, csb.PersistSecurityInfo);
			Assert.Equal(false, csb.Pooling);
			Assert.Equal(1234u, csb.Port);
			Assert.Equal("db-server", csb.Server);
			Assert.Equal(false, csb.TreatTinyAsBoolean);
			Assert.Equal(MySqlSslMode.VerifyCA, csb.SslMode);
			Assert.Equal(false, csb.UseAffectedRows);
			Assert.Equal(true, csb.UseCompression);
			Assert.Equal("username", csb.UserID);
		}

#if !BASELINE
		[Fact]
		public void SslModePreferredInvalidOperation()
		{
			var csb = new MySqlConnectionStringBuilder("ssl mode=preferred;");
			Assert.Throws<InvalidOperationException>(() => csb.SslMode);
		}
#endif
	}
}
