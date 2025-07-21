namespace IntegrationTests;

public class CommandTests : IClassFixture<DatabaseFixture>
{
	public CommandTests(DatabaseFixture database)
	{
		m_database = database;
	}

	[Fact]
	public void CommandTextIsEmptyStringByDefault()
	{
		using var command = new MySqlCommand();
		Assert.Equal("", command.CommandText);
	}

	[Fact]
	public void InitializeWithNullCommandText()
	{
		using var command = new MySqlCommand(default(string));
		Assert.Equal("", command.CommandText);
	}

	[Fact]
	public void SetCommandTextToNull()
	{
		using var command = new MySqlCommand();
		command.CommandText = null;
		Assert.Equal("", command.CommandText);
	}

	[Fact]
	public void SetCommandTextToEmptyString()
	{
		using var command = new MySqlCommand();
		command.CommandText = "";
		Assert.Equal("", command.CommandText);
	}

	[Fact]
	public void CreateCommandSetsConnection()
	{
		using var command = m_database.Connection.CreateCommand();
		Assert.Equal(m_database.Connection, command.Connection);
	}

#if !MYSQL_DATA
	[Fact]
	public void SingleQueryWithPrepareReuse()
	{
		using var connection = new MySqlConnection(m_database.Connection.ConnectionString);
		connection.Open();
		using (var cmd = connection.CreateCommand())
		{
			cmd.CommandText = "select ?";
			cmd.Prepare();
			cmd.Parameters.Add(new() { Value = 1 });
			using (var reader = cmd.ExecuteReader())
			{
				Assert.True(reader.Read());
				Assert.Equal(1, reader.GetInt32(0));
				Assert.False(reader.Read());
			}

			cmd.Parameters.Clear();
			cmd.Parameters.Add(new() { Value = 100 });
			using (var reader = cmd.ExecuteReader())
			{
				Assert.True(reader.Read());
				Assert.Equal(100, reader.GetInt32(0));
				Assert.False(reader.Read());
			}
		}

		using (var cmd = connection.CreateCommand())
		{
			cmd.CommandText = "select ?";
			cmd.Prepare();
			cmd.Parameters.Add(new() { Value = 2 });
			using var reader = cmd.ExecuteReader();
			Assert.True(reader.Read());
			Assert.Equal(2, reader.GetInt32(0));
			Assert.False(reader.Read());
		}
	}

	[Fact]
	public void MultiQueryWithPrepareReuse()
	{
		using var connection = new MySqlConnection(m_database.Connection.ConnectionString);
		connection.Open();
		using (var cmd = connection.CreateCommand())
		{
			cmd.CommandText = "select ?; select ? + 2";
			cmd.Prepare();
			cmd.Parameters.Add(new() { Value = 1 });
			cmd.Parameters.Add(new() { Value = 4 });
			using (var reader = cmd.ExecuteReader())
			{
				Assert.True(reader.Read());
				Assert.Equal(1, reader.GetInt32(0));
				Assert.False(reader.Read());

				Assert.True(reader.NextResult());

				Assert.True(reader.Read());
				Assert.Equal(6, reader.GetInt32(0));
				Assert.False(reader.Read());

				Assert.False(reader.NextResult());
			}

			cmd.Parameters.Clear();
			cmd.Parameters.Add(new() { Value = 100 });
			cmd.Parameters.Add(new() { Value = 400 });
			using (var reader = cmd.ExecuteReader())
			{
				Assert.True(reader.Read());
				Assert.Equal(100, reader.GetInt32(0));
				Assert.False(reader.Read());

				Assert.True(reader.NextResult());

				Assert.True(reader.Read());
				Assert.Equal(402, reader.GetInt32(0));
				Assert.False(reader.Read());

				Assert.False(reader.NextResult());
			}
		}

		using (var cmd = connection.CreateCommand())
		{
			cmd.CommandText = "select ?; select ? + 2";
			cmd.Prepare();
			cmd.Parameters.Add(new() { Value = 2 });
			cmd.Parameters.Add(new() { Value = 5 });
			using var reader = cmd.ExecuteReader();

			Assert.True(reader.Read());
			Assert.Equal(2, reader.GetInt32(0));
			Assert.False(reader.Read());

			Assert.True(reader.NextResult());

			Assert.True(reader.Read());
			Assert.Equal(7, reader.GetInt32(0));
			Assert.False(reader.Read());

			Assert.False(reader.NextResult());
		}
	}
#endif

	[Fact]
	public void CreateCommandDoesNotSetTransaction()
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();
		using var transaction = connection.BeginTransaction();
		using var cmd = connection.CreateCommand();
		Assert.Null(cmd.Transaction);
	}

	[Fact]
	public void ExecuteReaderRequiresConnection()
	{
		using var command = new MySqlCommand();
		Assert.Throws<InvalidOperationException>(() => command.ExecuteReader());
	}

	[Fact]
	public void ExecuteReaderRequiresOpenConnection()
	{
		using var connection = new MySqlConnection();
		using var command = connection.CreateCommand();
		Assert.Throws<InvalidOperationException>(() => command.ExecuteReader());
	}

	[Fact]
	public void PrepareRequiresConnection()
	{
		using var command = new MySqlCommand();
		Assert.Throws<InvalidOperationException>(() => command.Prepare());
	}

	[Fact]
	public void PrepareRequiresOpenConnection()
	{
		using var connection = new MySqlConnection();
		using var command = connection.CreateCommand();
		Assert.Throws<InvalidOperationException>(() => command.Prepare());
	}

	[Fact]
	public void NewCommandIsNotPrepared()
	{
		using var command = new MySqlCommand();
		Assert.False(command.IsPrepared);
	}

	[Fact]
	public void CommandWithoutConnectionIsNotPrepared()
	{
		using var command = new MySqlCommand();
		command.CommandText = "SELECT 1";
		Assert.False(command.IsPrepared);
	}

	[Fact]
	public void CommandWithClosedConnectionIsNotPrepared()
	{
		using var connection = new MySqlConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "SELECT 1";
		Assert.False(command.IsPrepared);
	}

	[Fact]
	public void ExecuteNonQueryForSelectReturnsNegativeOne()
	{
		using var connection = new MySqlConnection(m_database.Connection.ConnectionString);
		using var command = connection.CreateCommand();
		connection.Open();
		command.CommandText = "SELECT 1;";
		Assert.Equal(-1, command.ExecuteNonQuery());
	}

	[Fact]
	public async Task ExecuteNonQueryReturnValue()
	{
		using var connection = new MySqlConnection(m_database.Connection.ConnectionString);
		await connection.OpenAsync();
		const string setUp = @"drop table if exists execute_non_query;
create table execute_non_query(id integer not null primary key auto_increment, value text null);";
		await connection.ExecuteAsync(setUp);

		const string insert = "insert into execute_non_query(value) values(null), (null), ('one'), ('two');";
		Assert.Equal(4, await connection.ExecuteAsync(insert));
		const string select = "select value from execute_non_query;";
		Assert.Equal(-1, await connection.ExecuteAsync(select));
		const string delete = "delete from execute_non_query where value is null;";
		Assert.Equal(2, await connection.ExecuteAsync(delete));
		const string update = "update execute_non_query set value = 'three' where value = 'one';";
		Assert.Equal(1, await connection.ExecuteAsync(update));

		await connection.ExecuteAsync(setUp);
		Assert.Equal(7, await connection.ExecuteAsync(insert + select + delete + update));
	}

	[SkippableFact(MySqlData = "https://bugs.mysql.com/bug.php?id=88611")]
	public void CommandTransactionMustBeSet()
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();
		using var transaction = connection.BeginTransaction();
		using var command = connection.CreateCommand();
		command.CommandText = "SELECT 1;";
		Assert.Throws<InvalidOperationException>(() => command.ExecuteScalar());

		command.Transaction = transaction;
		TestUtilities.AssertIsOne(command.ExecuteScalar());
	}

	[Fact]
	public void IgnoreCommandTransactionIgnoresNull()
	{
		using var connection = new MySqlConnection(GetIgnoreCommandTransactionConnectionString());
		connection.Open();
		using var ignoredTransaction = connection.BeginTransaction();
		using var command = connection.CreateCommand();
		command.CommandText = "SELECT 1;";
		TestUtilities.AssertIsOne(command.ExecuteScalar());
	}

	[Fact]
	public void IgnoreCommandTransactionIgnoresDisposedTransaction()
	{
		using var connection = new MySqlConnection(GetIgnoreCommandTransactionConnectionString());
		connection.Open();

		var transaction = connection.BeginTransaction();
		transaction.Commit();
		transaction.Dispose();

		using var command = connection.CreateCommand();
		command.CommandText = "SELECT 1;";
		command.Transaction = transaction;
		TestUtilities.AssertIsOne(command.ExecuteScalar());
	}

	[Fact]
	public void IgnoreCommandTransactionIgnoresDifferentTransaction()
	{
		using var connection1 = new MySqlConnection(AppConfig.ConnectionString);
		using var connection2 = new MySqlConnection(GetIgnoreCommandTransactionConnectionString());
		connection1.Open();
		connection2.Open();

		using var transaction1 = connection1.BeginTransaction();
		using var command2 = connection2.CreateCommand();
		command2.Transaction = transaction1;
		command2.CommandText = "SELECT 1;";
		TestUtilities.AssertIsOne(command2.ExecuteScalar());
	}

	[Fact]
	public void ThrowsIfNamedParameterUsedButNoParametersDefined()
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();
		using var cmd = new MySqlCommand("SELECT @param;", connection);
		Assert.Throws<MySqlException>(() => cmd.ExecuteScalar());
	}

	[Fact]
	public void ThrowsIfUnnamedParameterUsedButNoParametersDefined()
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();
		using var cmd = new MySqlCommand("SELECT ?;", connection);
#if MYSQL_DATA
		Assert.Throws<IndexOutOfRangeException>(() => cmd.ExecuteScalar());
#else
		Assert.Throws<MySqlException>(() => cmd.ExecuteScalar());
#endif
	}

	[Fact]
	public void ThrowsIfUndefinedNamedParameterUsed()
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();
		using var cmd = new MySqlCommand("SELECT @param;", connection);
		cmd.Parameters.AddWithValue("@name", "test");
		Assert.Throws<MySqlException>(() => cmd.ExecuteScalar());
	}

	[Fact]
	public void ThrowsIfTooManyUnnamedParametersUsed()
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();
		using var cmd = new MySqlCommand("SELECT ?, ?;", connection);
		cmd.Parameters.Add(new() { Value = 1 });
#if MYSQL_DATA
		Assert.Throws<IndexOutOfRangeException>(() => cmd.ExecuteScalar());
#else
		Assert.Throws<MySqlException>(() => cmd.ExecuteScalar());
#endif
	}

	[Fact]
	public void CloneCommand()
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();
		using var transaction = connection.BeginTransaction();
		var param = new MySqlParameter("@param", MySqlDbType.Decimal) { Value = 12.3m };
		var attr = new MySqlAttribute("attr_name", 1.23);
		using var cmd = new MySqlCommand("SELECT @param;", connection, transaction)
		{
			CommandType = CommandType.StoredProcedure,
			Parameters = { param },
#if !MYSQL_DATA
			Attributes = { attr },
#endif
		};
#if MYSQL_DATA
		cmd.Attributes.SetAttribute(attr);
#endif
		using var cmd2 = (MySqlCommand) cmd.Clone();

		Assert.Equal(cmd.Connection, cmd2.Connection);
		Assert.Equal(cmd.Transaction, cmd2.Transaction);
		Assert.Equal(cmd.CommandText, cmd2.CommandText);
		Assert.Equal(cmd.CommandType, cmd2.CommandType);
		var param2 = (MySqlParameter) Assert.Single(cmd2.Parameters);

		Assert.Equal(param.ParameterName, param2.ParameterName);
		Assert.Equal(param.MySqlDbType, param2.MySqlDbType);
		Assert.Equal(param.Value, param2.Value);

		cmd.CommandText = "New text";
		Assert.NotEqual(cmd.CommandText, cmd2.CommandText);

		param.Value = 0m;
		Assert.NotEqual(0m, cmd2.Parameters[0].Value);

		Assert.Equal(1, cmd2.Attributes.Count);
		var attr2 = cmd2.Attributes[0];
		Assert.Equal(attr.AttributeName, attr2.AttributeName);
		Assert.Equal(attr.Value, attr2.Value);

		attr.Value = 0;
		Assert.NotEqual(0, cmd2.Attributes[0].Value);
	}

	[Fact]
	public void CancelEmptyCommandIsNoop()
	{
		using var cmd = new MySqlCommand();
		cmd.Cancel();
	}

	[Fact]
	public void CancelCommandForClosedConnectionIsNoop()
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();
		using var cmd = connection.CreateCommand();
		connection.Close();
		cmd.Cancel();
	}

	[Fact]
	public void CancelCommandForDisposedConnectionIsNoop()
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();
		using var cmd = connection.CreateCommand();
		connection.Dispose();
		cmd.Cancel();
	}

	[Fact]
	public void CommandsAreIndependent()
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();

		using var cmd1 = connection.CreateCommand();
		cmd1.CommandText = "SELECT 1;";

		using var cmd2 = connection.CreateCommand();
		cmd2.CommandText = "SELECT 'abc';";
		using var reader = cmd2.ExecuteReader();

		cmd1.Dispose();
		Assert.True(reader.Read());
	}

	[Fact]
	public void ExecutingCommandsAreIndependent()
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();

		using var cmd1 = connection.CreateCommand();
		cmd1.CommandText = "SELECT 1;";
		using var reader1 = cmd1.ExecuteReader();

		using var cmd2 = connection.CreateCommand();
		cmd2.CommandText = "SELECT 'abc';";

#if MYSQL_DATA
		Assert.Throws<MySqlException>(() => cmd2.ExecuteReader());
#else
		Assert.Throws<InvalidOperationException>(() => cmd2.ExecuteReader());
#endif
	}

	private static string GetIgnoreCommandTransactionConnectionString()
	{
#if MYSQL_DATA
		return AppConfig.ConnectionString;
#else
		return new MySqlConnectionStringBuilder(AppConfig.ConnectionString)
		{
			IgnoreCommandTransaction = true,
		}.ConnectionString;
#endif
	}

	private readonly DatabaseFixture m_database;
}
