using System;
using System.Linq;
#if BASELINE
using MySql.Data.MySqlClient;
#endif
using Xunit;

namespace MySqlConnector.Tests
{
	public class MySqlConnectionStringBuilderTests
	{
		[Fact]
		public void Defaults()
		{
			var csb = new MySqlConnectionStringBuilder();
			Assert.False(csb.AllowLoadLocalInfile);
			Assert.False(csb.AllowPublicKeyRetrieval);
			Assert.False(csb.AllowUserVariables);
			Assert.False(csb.AllowZeroDateTime);
			Assert.True(csb.AutoEnlist);
#if BASELINE
			Assert.Null(csb.CertificateFile);
			Assert.Null(csb.CertificatePassword);
			Assert.Null(csb.CertificateThumbprint);
#else
			Assert.Equal(2, csb.CancellationTimeout);
			Assert.Equal("", csb.CertificateFile);
			Assert.Equal("", csb.CertificatePassword);
			Assert.Equal("", csb.CertificateThumbprint);
#endif
			Assert.Equal(MySqlCertificateStoreLocation.None, csb.CertificateStoreLocation);
			Assert.Equal("", csb.CharacterSet);
			Assert.Equal(0u, csb.ConnectionLifeTime);
			Assert.Equal(MySqlConnectionProtocol.Sockets, csb.ConnectionProtocol);
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
			Assert.Equal("", csb.ApplicationName);
			Assert.Equal(0u, csb.ConnectionIdlePingTime);
			Assert.Equal(180u, csb.ConnectionIdleTimeout);
			Assert.False(csb.ForceSynchronous);
			Assert.Equal(MySqlGuidFormat.Default, csb.GuidFormat);
			Assert.False(csb.IgnoreCommandTransaction);
			Assert.Equal(MySqlLoadBalance.RoundRobin, csb.LoadBalance);
#endif
			Assert.True(csb.IgnorePrepare);
			Assert.False(csb.InteractiveSession);
			Assert.Equal(0u, csb.Keepalive);
			Assert.Equal(100u, csb.MaximumPoolSize);
			Assert.Equal(0u, csb.MinimumPoolSize);
			Assert.Equal("", csb.Password);
			Assert.Equal("MYSQL", csb.PipeName);
#if !BASELINE
			Assert.False(csb.NoBackslashEscapes);
#endif
			Assert.False(csb.OldGuids);
			Assert.False(csb.PersistSecurityInfo);
			Assert.True(csb.Pooling);
			Assert.Equal(3306u, csb.Port);
			Assert.Equal("", csb.Server);
#if !BASELINE
			Assert.Equal("", csb.ServerRsaPublicKeyFile);
			Assert.Equal("", csb.ServerSPN);
			Assert.Equal("", csb.SslCa);
			Assert.Equal("", csb.SslCert);
			Assert.Equal("", csb.SslKey);
			Assert.Equal("", csb.TlsVersion);
#else
			Assert.Null(csb.SslCa);
			Assert.Null(csb.SslCert);
			Assert.Null(csb.SslKey);
			Assert.Null(csb.TlsVersion);
#endif
			Assert.Equal(MySqlSslMode.Preferred, csb.SslMode);
			Assert.True(csb.TreatTinyAsBoolean);
			Assert.False(csb.UseCompression);
			Assert.Equal("", csb.UserID);
			Assert.False(csb.UseAffectedRows);
#if !BASELINE
			Assert.True(csb.UseXaTransactions);
#endif
		}

		[Fact]
		public void ParseConnectionString()
		{
			var csb = new MySqlConnectionStringBuilder
			{
				ConnectionString = "Data Source=db-server;" +
					"Initial Catalog=schema_name;" +
					"allow load local infile=true;" +
					"allowpublickeyretrieval = true;" +
					"Allow User Variables=true;" +
					"allow zero datetime=true;" +
					"auto enlist=False;" +
					"certificate file=file.pfx;" +
					"certificate password=Pass1234;" +
					"certificate store location=CurrentUser;" +
					"certificate thumb print=thumbprint123;" +
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
					"application name=My Test Application;" +
					"cancellation timeout = -1;" +
					"connection idle ping time=60;" +
					"connectionidletimeout=30;" +
					"forcesynchronous=true;" +
					"ignore command transaction=true;" +
					"server rsa public key file=rsa.pem;" +
					"load balance=random;" +
					"guidformat=timeswapbinary16;" +
					"nobackslashescapes=true;" +
					"server spn=mariadb/host.example.com@EXAMPLE.COM;" +
					"use xa transactions=false;" +
#endif
					"ignore prepare=false;" +
					"interactive=true;" +
					"Keep Alive=90;" +
					"minpoolsize=5;" +
					"maxpoolsize=15;" +
					"OldGuids=true;" +
					"persistsecurityinfo=yes;" +
					"Pipe=MyPipe;" +
					"Pooling=no;" +
					"Port=1234;" +
					"protocol=pipe;" +
					"pwd=Pass1234;" +
					"Treat Tiny As Boolean=false;" +
					"ssl-ca=ca.pem;" +
					"ssl-cert=client-cert.pem;" +
					"ssl-key=client-key.pem;" +
					"ssl mode=verifyca;" +
					"Uid=username;" +
					"useaffectedrows=true"
			};
			Assert.True(csb.AllowLoadLocalInfile);
			Assert.True(csb.AllowPublicKeyRetrieval);
			Assert.True(csb.AllowUserVariables);
			Assert.True(csb.AllowZeroDateTime);
			Assert.False(csb.AutoEnlist);
#if !BASELINE
			Assert.Equal(-1, csb.CancellationTimeout);
			// Connector/NET treats "CertificateFile" (client certificate) and "SslCa" (server CA) as aliases
			Assert.Equal("file.pfx", csb.CertificateFile);
#endif
			Assert.Equal("Pass1234", csb.CertificatePassword);
			Assert.Equal(MySqlCertificateStoreLocation.CurrentUser, csb.CertificateStoreLocation);
			Assert.Equal("thumbprint123", csb.CertificateThumbprint);
			Assert.Equal("latin1", csb.CharacterSet);
			Assert.Equal(15u, csb.ConnectionLifeTime);
			Assert.Equal(MySqlConnectionProtocol.NamedPipe, csb.ConnectionProtocol);
			Assert.False(csb.ConnectionReset);
			Assert.Equal(30u, csb.ConnectionTimeout);
			Assert.True(csb.ConvertZeroDateTime);
#if !BASELINE
			Assert.Equal(MySqlDateTimeKind.Utc, csb.DateTimeKind);
#endif
			Assert.Equal("schema_name", csb.Database);
			Assert.Equal(123u, csb.DefaultCommandTimeout);
#if !BASELINE
			Assert.Equal("My Test Application", csb.ApplicationName);
			Assert.Equal(60u, csb.ConnectionIdlePingTime);
			Assert.Equal(30u, csb.ConnectionIdleTimeout);
			Assert.True(csb.ForceSynchronous);
			Assert.True(csb.IgnoreCommandTransaction);
			Assert.Equal("rsa.pem", csb.ServerRsaPublicKeyFile);
			Assert.Equal(MySqlLoadBalance.Random, csb.LoadBalance);
			Assert.Equal(MySqlGuidFormat.TimeSwapBinary16, csb.GuidFormat);
			Assert.True(csb.NoBackslashEscapes);
			Assert.Equal("mariadb/host.example.com@EXAMPLE.COM", csb.ServerSPN);
			Assert.False(csb.UseXaTransactions);
#endif
			Assert.False(csb.IgnorePrepare);
			Assert.True(csb.InteractiveSession);
			Assert.Equal(90u, csb.Keepalive);
			Assert.Equal(15u, csb.MaximumPoolSize);
			Assert.Equal(5u, csb.MinimumPoolSize);
			Assert.Equal("Pass1234", csb.Password);
			Assert.Equal("MyPipe", csb.PipeName);
			Assert.True(csb.OldGuids);
			Assert.True(csb.PersistSecurityInfo);
			Assert.False(csb.Pooling);
			Assert.Equal(1234u, csb.Port);
			Assert.Equal("db-server", csb.Server);
			Assert.False(csb.TreatTinyAsBoolean);
			Assert.Equal("ca.pem", csb.SslCa);
			Assert.Equal("client-cert.pem", csb.SslCert);
			Assert.Equal("client-key.pem", csb.SslKey);
			Assert.Equal(MySqlSslMode.VerifyCA, csb.SslMode);
			Assert.True(csb.UseAffectedRows);
			Assert.True(csb.UseCompression);
			Assert.Equal("username", csb.UserID);
		}

		[Fact]
		public void EnumInvalidOperation()
		{
			Assert.Throws<ArgumentException>(() => new MySqlConnectionStringBuilder("ssl mode=invalid;"));
		}

#if !BASELINE
		[Fact]
		public void ConstructWithNull()
		{
			var csb = new MySqlConnectionStringBuilder(default(string));
			Assert.Equal("", csb.ConnectionString);
		}
#endif

		[Fact]
		public void ConstructWithEmptyString()
		{
			var csb = new MySqlConnectionStringBuilder("");
			Assert.Equal("", csb.ConnectionString);
		}

		[Fact]
		public void SetConnectionStringToNull()
		{
			var csb = new MySqlConnectionStringBuilder
			{
				ConnectionString = null,
			};
			Assert.Equal("", csb.ConnectionString);
		}

		[Fact]
		public void SetConnectionStringToEmptyString()
		{
			var csb = new MySqlConnectionStringBuilder
			{
				ConnectionString = "",
			};
			Assert.Equal("", csb.ConnectionString);
		}

		[Fact]
		public void SetServerToNull()
		{
			var csb = new MySqlConnectionStringBuilder("Server=test");
			csb.Server = null;
			Assert.Equal("", csb.ConnectionString);
		}

		[Fact]
		public void SetUserIdToNull()
		{
			var csb = new MySqlConnectionStringBuilder("User ID=test");
			csb.UserID = null;
			Assert.Equal("", csb.ConnectionString);
		}

		[Fact]
		public void SetPasswordToNull()
		{
			var csb = new MySqlConnectionStringBuilder("Password=test");
			csb.Password = null;
			Assert.Equal("", csb.ConnectionString);
		}

		[Fact]
		public void SetDatabaseToNull()
		{
			var csb = new MySqlConnectionStringBuilder("Database=test");
			csb.Database = null;
			Assert.Equal("", csb.ConnectionString);
		}

		[Fact]
		public void SetPipeNameToNull()
		{
			var csb = new MySqlConnectionStringBuilder("Pipe=test");
			csb.PipeName = null;
			Assert.Equal("", csb.ConnectionString);
		}

		[Fact]
		public void SetCertificateFileToNull()
		{
			var csb = new MySqlConnectionStringBuilder("CertificateFile=test");
			csb.CertificateFile = null;
			Assert.Equal("", csb.ConnectionString);
		}

		[Fact]
		public void SetCertificatePasswordToNull()
		{
			var csb = new MySqlConnectionStringBuilder("CertificatePassword=test");
			csb.CertificatePassword = null;
			Assert.Equal("", csb.ConnectionString);
		}

		[Fact]
		public void SetSslCaToNull()
		{
			var csb = new MySqlConnectionStringBuilder("SslCa=test");
			csb.SslCa = null;
			Assert.Equal("", csb.ConnectionString);
		}

		[Fact]
		public void SetSslCertToNull()
		{
			var csb = new MySqlConnectionStringBuilder("SslCert=test");
			csb.SslCert = null;
			Assert.Equal("", csb.ConnectionString);
		}

		[Fact]
		public void SetSslKeyToNull()
		{
			var csb = new MySqlConnectionStringBuilder("SslKey=test");
			csb.SslKey = null;
			Assert.Equal("", csb.ConnectionString);
		}

		[Fact]
		public void SetCertificateThumbprintToNull()
		{
			var csb = new MySqlConnectionStringBuilder("CertificateThumbprint=test");
			csb.CertificateThumbprint = null;
			Assert.Equal("", csb.ConnectionString);
		}

		[Fact]
		public void SetCharacterSetToNull()
		{
			var csb = new MySqlConnectionStringBuilder("CharSet=test");
			csb.CharacterSet = null;
			Assert.Equal("", csb.ConnectionString);
		}

#if !BASELINE
		[Fact]
		public void SetApplicationNameToNull()
		{
			var csb = new MySqlConnectionStringBuilder("ApplicationName=test");
			csb.ApplicationName = null;
			Assert.Equal("", csb.ConnectionString);
		}

		[Fact]
		public void SetServerRsaPublicKeyFileToNull()
		{
			var csb = new MySqlConnectionStringBuilder("ServerRSAPublicKeyFile=test");
			csb.ServerRsaPublicKeyFile = null;
			Assert.Equal("", csb.ConnectionString);
		}

		[Fact]
		public void SetServerSPNToNull()
		{
			var csb = new MySqlConnectionStringBuilder("ServerSPN=test");
			csb.ServerSPN = null;
			Assert.Equal("", csb.ConnectionString);
		}
#endif

		[Theory]
		[InlineData("Tls", "0")]
		[InlineData("Tls1", "0")]
		[InlineData("Tlsv1", "0")]
		[InlineData("Tlsv1.0", "0")]
		[InlineData("TLS 1.0", "0")]
		[InlineData("TLS v1.0", "0")]
		[InlineData("Tls11", "1")]
		[InlineData("Tlsv11", "1")]
		[InlineData("Tlsv1.1", "1")]
		[InlineData("TLS 1.1", "1")]
		[InlineData("TLS v1.1", "1")]
		[InlineData("Tls12", "2")]
		[InlineData("Tlsv12", "2")]
		[InlineData("Tlsv1.2", "2")]
		[InlineData("TLS 1.2", "2")]
		[InlineData("TLS v1.2", "2")]
		[InlineData("Tls13", "3")]
		[InlineData("Tlsv13", "3")]
		[InlineData("Tlsv1.3", "3")]
		[InlineData("TLS 1.3", "3")]
		[InlineData("TLS v1.3", "3")]
		[InlineData("Tls,Tls", "0")]
		[InlineData("Tls1.1,Tls v1.1, TLS 1.1", "1")]
		[InlineData("Tls12,Tls10", "0,2")]
		[InlineData("TLS v1.3, TLS12, Tls 1.1", "1,2,3")]
		public void ParseTlsVersion(string input, string expected)
		{
			var csb = new MySqlConnectionStringBuilder { TlsVersion = input };
#if !BASELINE
			string[] normalizedVersions = new[] { "TLS 1.0", "TLS 1.1", "TLS 1.2", "TLS 1.3" };
#else
			string[] normalizedVersions = new[] { "Tls", "Tls11", "Tls12", "Tls13" };
#endif
			var expectedTlsVersion = string.Join(", ", expected.Split(',').Select(int.Parse).Select(x => normalizedVersions[x]));
			Assert.Equal(expectedTlsVersion, csb.TlsVersion);
		}

		[Fact]
		public void ParseInvalidTlsVersion()
		{
			var csb = new MySqlConnectionStringBuilder();
			Assert.Throws<ArgumentException>(() => csb.TlsVersion = "Tls14");
			Assert.Throws<ArgumentException>(() => new MySqlConnectionStringBuilder("TlsVersion=Tls14"));
		}
	}
}
