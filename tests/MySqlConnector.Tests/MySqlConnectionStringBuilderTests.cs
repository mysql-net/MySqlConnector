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
#if !BASELINE
			Assert.False(csb.AllowPublicKeyRetrieval);
#endif
			Assert.False(csb.AllowUserVariables);
			Assert.True(csb.AutoEnlist);
			Assert.Null(csb.CertificateFile);
			Assert.Null(csb.CertificatePassword);
			Assert.Equal("", csb.CharacterSet);
			Assert.Equal(0u, csb.ConnectionLifeTime);
#if BASELINE
			Assert.False(csb.ConnectionReset);
#else
			Assert.True(csb.ConnectionReset);
#endif
			Assert.Equal(15u, csb.ConnectionTimeout);
			Assert.False(csb.ConvertZeroDateTime);
			Assert.Equal("", csb.Database);
			Assert.Equal(30u, csb.DefaultCommandTimeout);
#if !BASELINE
			Assert.Equal(180u, csb.ConnectionIdleTimeout);
			Assert.False(csb.ForceSynchronous);
			Assert.Null(csb.CACertificateFile);
#endif
			Assert.Equal(0u, csb.Keepalive);
			Assert.Equal(100u, csb.MaximumPoolSize);
			Assert.Equal(0u, csb.MinimumPoolSize);
			Assert.Equal("", csb.Password);
			Assert.False(csb.OldGuids);
			Assert.False(csb.PersistSecurityInfo);
			Assert.True(csb.Pooling);
			Assert.Equal(3306u, csb.Port);
			Assert.Equal("", csb.Server);
#if !BASELINE
			Assert.Null(csb.ServerRsaPublicKeyFile);
#endif
			Assert.Equal(MySqlSslMode.Preferred, csb.SslMode);
			Assert.True(csb.TreatTinyAsBoolean);
			Assert.False(csb.UseCompression);
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
					"auto enlist=False;" +
					"certificate file=file.pfx;" +
					"certificate password=Pass1234;" +
					"Character Set=latin1;" +
					"Compress=true;" +
					"connect timeout=30;" +
					"connection lifetime=15;" +
					"ConnectionReset=false;" +
					"Convert Zero Datetime=true;" +
					"default command timeout=123;" +
#if !BASELINE
					"connectionidletimeout=30;" +
					"forcesynchronous=true;" +
					"ca certificate file=ca.pem;" +
					"allow public key retrieval = true;" +
					"server rsa public key file=rsa.pem;" +
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
			Assert.True(csb.AllowUserVariables);
			Assert.False(csb.AutoEnlist);
			Assert.Equal("file.pfx", csb.CertificateFile);
			Assert.Equal("Pass1234", csb.CertificatePassword);
			Assert.Equal("latin1", csb.CharacterSet);
			Assert.Equal(15u, csb.ConnectionLifeTime);
			Assert.False(csb.ConnectionReset);
			Assert.Equal(30u, csb.ConnectionTimeout);
			Assert.True(csb.ConvertZeroDateTime);
			Assert.Equal("schema_name", csb.Database);
			Assert.Equal(123u, csb.DefaultCommandTimeout);
#if !BASELINE
			Assert.Equal(30u, csb.ConnectionIdleTimeout);
			Assert.True(csb.ForceSynchronous);
			Assert.Equal("ca.pem", csb.CACertificateFile);
			Assert.True(csb.AllowPublicKeyRetrieval);
			Assert.Equal("rsa.pem", csb.ServerRsaPublicKeyFile);
#endif
			Assert.Equal(90u, csb.Keepalive);
			Assert.Equal(15u, csb.MaximumPoolSize);
			Assert.Equal(5u, csb.MinimumPoolSize);
			Assert.Equal("Pass1234", csb.Password);
			Assert.True(csb.OldGuids);
			Assert.True(csb.PersistSecurityInfo);
			Assert.False(csb.Pooling);
			Assert.Equal(1234u, csb.Port);
			Assert.Equal("db-server", csb.Server);
			Assert.False(csb.TreatTinyAsBoolean);
			Assert.Equal(MySqlSslMode.VerifyCA, csb.SslMode);
			Assert.False(csb.UseAffectedRows);
			Assert.True(csb.UseCompression);
			Assert.Equal("username", csb.UserID);
		}

#if !BASELINE
		[Fact]
		public void EnumInvalidOperation()
		{
			var csb = new MySqlConnectionStringBuilder("ssl mode=invalid;");
			Assert.Throws<InvalidOperationException>(() => csb.SslMode);
		}
#endif
	}
}
