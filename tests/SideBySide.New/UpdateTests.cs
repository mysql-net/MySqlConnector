using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;
using Xunit;

namespace SideBySide
{
	public class UpdateTests : IClassFixture<DatabaseFixture>, IDisposable
	{
		public UpdateTests(DatabaseFixture database)
		{
			m_database = database;
			m_database.Connection.Open();
		}

		public void Dispose()
		{
			m_database.Connection.Close();
		}

		[Theory]
		[InlineData(1, 2)]
		[InlineData(2, 1)]
		[InlineData(3, 0)]
		[InlineData(4, 0)]
		public async Task UpdateRowsExecuteReader(int oldValue, int expectedRowsUpdated)
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"drop schema if exists update_int;
create schema update_int;
create table update_int.test(id integer not null primary key auto_increment, value integer not null);
insert into update_int.test (value) VALUES (1), (2), (1), (4);
";
				cmd.ExecuteNonQuery();
			}

			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"update update_int.test set value = @newValue where value = @oldValue";
				var p = cmd.CreateParameter();
				p.ParameterName = "@oldValue";
				p.Value = oldValue;
				cmd.Parameters.Add(p);
				p = cmd.CreateParameter();
				p.ParameterName = "@newValue";
				p.Value = 4;
				cmd.Parameters.Add(p);

				using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
				{
					Assert.False(await reader.ReadAsync().ConfigureAwait(false));
					Assert.Equal(expectedRowsUpdated, reader.RecordsAffected);
					Assert.False(await reader.NextResultAsync().ConfigureAwait(false));
				}
			}
		}

		[Theory]
		[InlineData(1, 2)]
		[InlineData(2, 1)]
		[InlineData(3, 0)]
		[InlineData(4, 0)]
		public async Task UpdateRowsExecuteNonQuery(int oldValue, int expectedRowsUpdated)
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"drop schema if exists update_int;
create schema update_int;
create table update_int.test(id integer not null primary key auto_increment, value integer not null);
insert into update_int.test (value) VALUES (1), (2), (1), (4);
";
				cmd.ExecuteNonQuery();
			}

			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"update update_int.test set value = @newValue where value = @oldValue";
				var p = cmd.CreateParameter();
				p.ParameterName = "@oldValue";
				p.Value = oldValue;
				cmd.Parameters.Add(p);
				p = cmd.CreateParameter();
				p.ParameterName = "@newValue";
				p.Value = 4;
				cmd.Parameters.Add(p);

				var rowsAffected = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
				Assert.Equal(expectedRowsUpdated, rowsAffected);
			}
		}

		[Theory]
		[InlineData(1, 2)]
		[InlineData(2, 1)]
		[InlineData(3, 0)]
		[InlineData(4, 0)]
		public void UpdateRowsDapper(int oldValue, int expectedRowsUpdated)
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"drop schema if exists update_int;
create schema update_int;
create table update_int.test(id integer not null primary key auto_increment, value integer not null);
insert into update_int.test (value) VALUES (1), (2), (1), (4);
";
				cmd.ExecuteNonQuery();
			}

			var rowsAffected = m_database.Connection.Execute(@"update update_int.test set value = @newValue where value = @oldValue",
				new { oldValue, newValue = 4 });
			Assert.Equal(expectedRowsUpdated, rowsAffected);
		}

		[Theory]
		[InlineData(1, 2)]
		[InlineData(2, 1)]
		[InlineData(3, 0)]
		[InlineData(4, 0)]
		public async Task UpdateRowsDapperAsync(int oldValue, int expectedRowsUpdated)
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"drop schema if exists update_int;
create schema update_int;
create table update_int.test(id integer not null primary key auto_increment, value integer not null);
insert into update_int.test (value) VALUES (1), (2), (1), (4);
";
				cmd.ExecuteNonQuery();
			}

			var rowsAffected = await m_database.Connection.ExecuteAsync(@"update update_int.test set value = @newValue where value = @oldValue",
				new { oldValue, newValue = 4 }).ConfigureAwait(false);
			Assert.Equal(expectedRowsUpdated, rowsAffected);
		}

		readonly DatabaseFixture m_database;
	}
}
