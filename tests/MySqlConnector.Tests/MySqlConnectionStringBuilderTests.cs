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
			Assert.Equal("", csb.CharacterSet);
#if BASELINE
			Assert.Equal(false, csb.ConnectionReset);
#else
			Assert.Equal(true, csb.ConnectionReset);
#endif
			Assert.Equal(15u, csb.ConnectionTimeout);
#if BASELINE
			Assert.False(csb.UseAffectedRows);
#else
			Assert.True(csb.UseAffectedRows);
#endif
			Assert.Equal(false, csb.ConvertZeroDateTime);
			Assert.Equal("", csb.Database);
			Assert.Equal(100u, csb.MaximumPoolSize);
			Assert.Equal(0u, csb.MinimumPoolSize);
			Assert.Equal("", csb.Password);
			Assert.Equal(false, csb.OldGuids);
			Assert.Equal(false, csb.PersistSecurityInfo);
			Assert.Equal(true, csb.Pooling);
			Assert.Equal(3306u, csb.Port);
			Assert.Equal("", csb.Server);
			Assert.Equal(false, csb.UseCompression);
			Assert.Equal("", csb.UserID);
			Assert.Equal(SslMode.None, csb.SslMode);
			Assert.Equal("", csb.CertificateFile);
			Assert.Equal("", csb.CertificatePassword);
#if !BASELINE
			Assert.Equal(false, csb.ForceSynchronous);
#endif
		}

		[Fact]
		public void ParseConnectionString()
		{
			var csb = new MySqlConnectionStringBuilder
			{
				ConnectionString = "Data Source=db-server;Port=1234;Uid=username;pwd=Pass1234;Initial Catalog=schema_name;Allow User Variables=true;Character Set=latin1;Convert Zero Datetime=true;Pooling=no;OldGuids=true;Compress=true;ConnectionReset=false;minpoolsize=5;maxpoolsize=15;persistsecurityinfo=yes;useaffectedrows=false;connect timeout=30;ssl mode=verifyca;certificate file=file.pfx;certificate password=Pass1234"
#if !BASELINE
					+ ";forcesynchronous=true"
#endif
			};
			Assert.Equal(true, csb.AllowUserVariables);
			Assert.Equal("latin1", csb.CharacterSet);
			Assert.Equal(false, csb.ConnectionReset);
			Assert.Equal(30u, csb.ConnectionTimeout);
			Assert.Equal(true, csb.ConvertZeroDateTime);
			Assert.Equal("schema_name", csb.Database);
			Assert.Equal(15u, csb.MaximumPoolSize);
			Assert.Equal(5u, csb.MinimumPoolSize);
			Assert.Equal("Pass1234", csb.Password);
			Assert.Equal(true, csb.OldGuids);
			Assert.Equal(true, csb.PersistSecurityInfo);
			Assert.Equal(false, csb.Pooling);
			Assert.Equal(1234u, csb.Port);
			Assert.Equal("db-server", csb.Server);
			Assert.Equal(false, csb.UseAffectedRows);
			Assert.Equal(true, csb.UseCompression);
			Assert.Equal("username", csb.UserID);
			Assert.Equal(SslMode.VerifyCa, csb.SslMode);
			Assert.Equal("file.pfx", csb.CertificateFile);
			Assert.Equal("Pass1234", csb.CertificatePassword);
#if !BASELINE
			Assert.Equal(true, csb.ForceSynchronous);
#endif
		}

#if !BASELINE
		[Fact]
		public void UseCompressionNotSupported()
		{
			var csb = new MySqlConnectionStringBuilder
			{
				UseCompression = true,
			};
			Assert.Throws<NotSupportedException>(() => new MySqlConnection(csb.ConnectionString));
		}

		[Fact]
		public void UseAffectedRowsNotSupported()
		{
			var csb = new MySqlConnectionStringBuilder
			{
				UseAffectedRows = false,
			};
			Assert.Throws<NotSupportedException>(() => new MySqlConnection(csb.ConnectionString));
		}

		[Fact]
		public void SslModePreferredInvalidOperation()
		{
			var csb = new MySqlConnectionStringBuilder("ssl mode=preferred;");
			Assert.Throws<InvalidOperationException>(() => csb.SslMode);
		}
#endif
	}
}
