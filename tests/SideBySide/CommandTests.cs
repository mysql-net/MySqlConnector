using System;
using MySql.Data.MySqlClient;
using Xunit;

namespace SideBySide
{
	public class CommandTests : IClassFixture<DatabaseFixture>
	{
		public CommandTests(DatabaseFixture database)
		{
			m_database = database;
		}

		[Fact]
		public void CreateCommandSetsConnection()
		{
			using (var command = m_database.Connection.CreateCommand())
			{
				Assert.Equal(m_database.Connection, command.Connection);
			}
		}

		[Fact]
		public void ExecuteReaderRequiresConnection()
		{
			using (var command = new MySqlCommand())
			{
				Assert.Throws<InvalidOperationException>(() => command.ExecuteReader());
			}
		}

		[Fact]
		public void ExecuteReaderRequiresOpenConnection()
		{
			using (var connection = new MySqlConnection())
			using (var command = connection.CreateCommand())
			{
				Assert.Throws<InvalidOperationException>(() => command.ExecuteReader());
			}
		}

		[Fact]
		public void PrepareRequiresConnection()
		{
			using (var command = new MySqlCommand())
			{
				Assert.Throws<InvalidOperationException>(() => command.Prepare());
			}
		}

		[Fact]
		public void PrepareRequiresOpenConnection()
		{
			using (var connection = new MySqlConnection())
			using (var command = connection.CreateCommand())
			{
				Assert.Throws<InvalidOperationException>(() => command.Prepare());
			}
		}

		readonly DatabaseFixture m_database;
	}
}
