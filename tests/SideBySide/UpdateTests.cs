using System;
using System.Threading.Tasks;
using Dapper;
#if BASELINE
using MySql.Data.MySqlClient;
#else
using MySqlConnector;
#endif
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
		[InlineData(4, 1)]
		public async Task UpdateRowsExecuteReader(int oldValue, int expectedRowsUpdated)
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"drop table if exists update_rows_reader;
create table update_rows_reader(id integer not null primary key auto_increment, value integer not null);
insert into update_rows_reader (value) VALUES (1), (2), (1), (4);
";
				cmd.ExecuteNonQuery();
			}

			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"update update_rows_reader set value = @newValue where value = @oldValue";
				var p = cmd.CreateParameter();
				p.ParameterName = "@oldValue";
				p.Value = oldValue;
				cmd.Parameters.Add(p);
				p = cmd.CreateParameter();
				p.ParameterName = "@newValue";
				p.Value = 4;
				cmd.Parameters.Add(p);

				using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
				Assert.False(await reader.ReadAsync().ConfigureAwait(false));
				Assert.Equal(expectedRowsUpdated, reader.RecordsAffected);
				Assert.False(await reader.NextResultAsync().ConfigureAwait(false));
			}
		}

		[Theory]
		[InlineData(1, 2)]
		[InlineData(2, 1)]
		[InlineData(3, 0)]
		[InlineData(4, 1)]
		public async Task UpdateRowsExecuteNonQuery(int oldValue, int expectedRowsUpdated)
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"drop table if exists update_rows_non_query;
create table update_rows_non_query(id integer not null primary key auto_increment, value integer not null);
insert into update_rows_non_query (value) VALUES (1), (2), (1), (4);
";
				cmd.ExecuteNonQuery();
			}

			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"update update_rows_non_query set value = @newValue where value = @oldValue";
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
		[InlineData(4, 1)]
		public void UpdateRowsDapper(int oldValue, int expectedRowsUpdated)
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"drop table if exists update_rows_dapper;
create table update_rows_dapper(id integer not null primary key auto_increment, value integer not null);
insert into update_rows_dapper (value) VALUES (1), (2), (1), (4);
";
				cmd.ExecuteNonQuery();
			}

			var rowsAffected = m_database.Connection.Execute(@"update update_rows_dapper set value = @newValue where value = @oldValue",
				new { oldValue, newValue = 4 });
			Assert.Equal(expectedRowsUpdated, rowsAffected);
		}

		[Theory]
		[InlineData(true, 1, 2)]
		[InlineData(true, 2, 1)]
		[InlineData(true, 3, 0)]
		[InlineData(true, 4, 0)]
		[InlineData(false, 1, 2)]
		[InlineData(false, 2, 1)]
		[InlineData(false, 3, 0)]
		[InlineData(false, 4, 1)]
		public async Task UpdateRowsDapperAsync(bool useAffectedRows, int oldValue, int expectedRowsUpdated)
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			csb.UseAffectedRows = useAffectedRows;
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				await connection.OpenAsync();
				using (var cmd = connection.CreateCommand())
				{
					cmd.CommandText = @"drop table if exists update_rows_dapper_async;
create table update_rows_dapper_async(id integer not null primary key auto_increment, value integer not null);
insert into update_rows_dapper_async (value) VALUES (1), (2), (1), (4);
";
					cmd.ExecuteNonQuery();
				}
				var rowsAffected = await connection.ExecuteAsync(@"update update_rows_dapper_async set value = @newValue where value = @oldValue",
					new { oldValue, newValue = 4 }).ConfigureAwait(false);
				Assert.Equal(expectedRowsUpdated, rowsAffected);
			}
		}

		[Fact]
		public void UpdateFieldCount()
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"drop table if exists update_rows_reader;
create table update_rows_reader(id integer not null primary key auto_increment, value text not null);
insert into update_rows_reader (value) VALUES ('one'), ('two'), ('one'), ('four');
";
				cmd.ExecuteNonQuery();
			}

			using (var cmd = new MySqlCommand(@"UPDATE update_rows_reader SET value = 'three' WHERE id = 3;", m_database.Connection))
			using (var reader = cmd.ExecuteReader())
			{
				Assert.Equal(0, reader.FieldCount);
				Assert.False(reader.HasRows);
				Assert.False(reader.Read());
			}
		}

		readonly DatabaseFixture m_database;
	}
}
