using MySql.Data.MySqlClient;
using Xunit;

namespace SideBySide
{
	public class ClientFactoryTests
	{
		[Fact]
		public void CreateCommand()
		{
			Assert.IsType<MySqlCommand>(MySqlClientFactory.Instance.CreateCommand());
		}

		[Fact]
		public void CreateConnection()
		{
			Assert.IsType<MySqlConnection>(MySqlClientFactory.Instance.CreateConnection());
		}

		[Fact]
		public void CreateConnectionStringBuilder()
		{
			Assert.IsType<MySqlConnectionStringBuilder>(MySqlClientFactory.Instance.CreateConnectionStringBuilder());
		}


		[Fact]
		public void CreateParameter()
		{
			Assert.IsType<MySqlParameter>(MySqlClientFactory.Instance.CreateParameter());
		}

#if !NETCOREAPP1_1_2
		[SkippableFact(Baseline = "https://bugs.mysql.com/bug.php?id=88660")]
		public void CreateCommandBuilder()
		{
			Assert.IsType<MySqlCommandBuilder>(MySqlClientFactory.Instance.CreateCommandBuilder());
		}

		[SkippableFact(Baseline = "https://bugs.mysql.com/bug.php?id=88660")]
		public void CreateDataAdapter()
		{
			Assert.IsType<MySqlDataAdapter>(MySqlClientFactory.Instance.CreateDataAdapter());
		}
#endif
	}
}
