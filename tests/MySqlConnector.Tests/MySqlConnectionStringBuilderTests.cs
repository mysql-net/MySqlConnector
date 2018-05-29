using System;
using MySql.Data.MySqlClient;
using Xunit;

namespace MySqlConnector.Tests
{
	public class MySqlConnectionStringBuilderTests
	{
		[Fact]
		public void Defaults()
		{
			var csb = new MySqlConnectionStringBuilder();
			Assert.False(csb.AllowPublicKeyRetrieval);
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
#if !BASELINE
			Assert.Equal(MySqlDateTimeKind.Unspecified, csb.DateTimeKind);
#endif
			Assert.Equal("", csb.Database);
			Assert.Equal(30u, csb.DefaultCommandTimeout);
#if !BASELINE
			Assert.Equal(0u, csb.ConnectionIdlePingTime);
			Assert.Equal(180u, csb.ConnectionIdleTimeout);
			Assert.False(csb.ForceSynchronous);
#if !BASELINE
			Assert.Equal(MySqlGuidFormat.Default, csb.GuidFormat);
#endif
			Assert.False(csb.IgnoreCommandTransaction);
			Assert.Null(csb.CACertificateFile);
			Assert.Equal(MySqlLoadBalance.RoundRobin, csb.LoadBalance);
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
#if !BASELINE
			Assert.Equal(MySqlSslMode.Preferred, csb.SslMode);
#else
			Assert.Equal(MySqlSslMode.Required, csb.SslMode);
#endif
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
					"allowpublickeyretrieval = true;" +
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
#if !BASELINE
					"datetimekind=utc;" +
#endif
					"default command timeout=123;" +
#if !BASELINE
					"connection idle ping time=60;" +
					"connectionidletimeout=30;" +
					"forcesynchronous=true;" +
					"ignore command transaction=true;" +
					"ca certificate file=ca.pem;" +
					"server rsa public key file=rsa.pem;" +
					"load balance=random;" +
					"guidformat=timeswapbinary16;" +
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
			Assert.True(csb.AllowPublicKeyRetrieval);
			Assert.True(csb.AllowUserVariables);
			Assert.False(csb.AutoEnlist);
			Assert.Equal("file.pfx", csb.CertificateFile);
			Assert.Equal("Pass1234", csb.CertificatePassword);
			Assert.Equal("latin1", csb.CharacterSet);
			Assert.Equal(15u, csb.ConnectionLifeTime);
			Assert.False(csb.ConnectionReset);
			Assert.Equal(30u, csb.ConnectionTimeout);
			Assert.True(csb.ConvertZeroDateTime);
#if !BASELINE
			Assert.Equal(MySqlDateTimeKind.Utc, csb.DateTimeKind);
#endif
			Assert.Equal("schema_name", csb.Database);
			Assert.Equal(123u, csb.DefaultCommandTimeout);
#if !BASELINE
			Assert.Equal(60u, csb.ConnectionIdlePingTime);
			Assert.Equal(30u, csb.ConnectionIdleTimeout);
			Assert.True(csb.ForceSynchronous);
			Assert.True(csb.IgnoreCommandTransaction);
			Assert.Equal("ca.pem", csb.CACertificateFile);
			Assert.Equal("rsa.pem", csb.ServerRsaPublicKeyFile);
			Assert.Equal(MySqlLoadBalance.Random, csb.LoadBalance);
			Assert.Equal(MySqlGuidFormat.TimeSwapBinary16, csb.GuidFormat);
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
