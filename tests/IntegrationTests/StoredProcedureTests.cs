namespace IntegrationTests;

public class StoredProcedureTests : IClassFixture<StoredProcedureFixture>
{
	public StoredProcedureTests(StoredProcedureFixture database)
	{
		m_database = database;
	}

	[Theory]
	[InlineData("FUNCTION", "NonQuery", false)]
	[InlineData("FUNCTION", "Scalar", false)]
	[InlineData("FUNCTION", "Reader", false)]
	[InlineData("PROCEDURE", "NonQuery", true)]
	[InlineData("PROCEDURE", "NonQuery", false)]
	[InlineData("PROCEDURE", "Scalar", true)]
	[InlineData("PROCEDURE", "Scalar", false)]
	[InlineData("PROCEDURE", "Reader", true)]
	[InlineData("PROCEDURE", "Reader", false)]
	public async Task StoredProcedureEcho(string procedureType, string executorType, bool prepare)
	{
		using var connection = CreateOpenConnection();
		using var cmd = connection.CreateCommand();
		cmd.CommandText = "echo" + (procedureType == "FUNCTION" ? "f" : "p");
		cmd.CommandType = CommandType.StoredProcedure;

		cmd.Parameters.Add(new()
		{
			ParameterName = "@name",
			DbType = DbType.String,
			Direction = ParameterDirection.Input,
			Value = "hello",
		});

		// we make the assumption that Stored Procedures with ParameterDirection.ReturnValue are functions
		if (procedureType == "FUNCTION")
		{
			cmd.Parameters.Add(new()
			{
				ParameterName = "@result",
				DbType = DbType.String,
				Direction = ParameterDirection.ReturnValue,
			});
		}

		if (prepare)
			await cmd.PrepareAsync();
		var result = await ExecuteCommandAsync(cmd, executorType);
		if (procedureType == "PROCEDURE" && executorType != "NonQuery")
			Assert.Equal(cmd.Parameters["@name"].Value, result);
		if (procedureType == "FUNCTION")
			Assert.Equal(cmd.Parameters["@name"].Value, cmd.Parameters["@result"].Value);
	}

	[Fact]
	public void CallFailingFunction()
	{
		using var command = m_database.Connection.CreateCommand();

		command.CommandType = CommandType.StoredProcedure;
		command.CommandText = "failing_function";

		var returnParameter = command.CreateParameter();
		returnParameter.DbType = DbType.Int32;
		returnParameter.Direction = ParameterDirection.ReturnValue;
		command.Parameters.Add(returnParameter);

		Assert.Throws<MySqlException>(() => command.ExecuteNonQuery());
	}

	[Fact]
	public void CallFailingFunctionInTransaction()
	{
		using var transaction = m_database.Connection.BeginTransaction();
		using var command = m_database.Connection.CreateCommand();
		command.Transaction = transaction;
		command.CommandType = CommandType.StoredProcedure;
		command.CommandText = "failing_function";

		var returnParameter = command.CreateParameter();
		returnParameter.DbType = DbType.Int32;
		returnParameter.Direction = ParameterDirection.ReturnValue;
		command.Parameters.Add(returnParameter);

		Assert.Throws<MySqlException>(() => command.ExecuteNonQuery());
		transaction.Commit();
	}

	[SkippableTheory(ServerFeatures.StoredProcedures)]
	[InlineData("FUNCTION", false)]
	[InlineData("PROCEDURE", true)]
	[InlineData("PROCEDURE", false)]
	public async Task StoredProcedureEchoException(string procedureType, bool prepare)
	{
		using var connection = CreateOpenConnection();
		using var cmd = connection.CreateCommand();
		cmd.CommandText = "echo" + (procedureType == "FUNCTION" ? "f" : "p");
		cmd.CommandType = CommandType.StoredProcedure;

		if (prepare)
		{
#if MYSQL_DATA
			await Assert.ThrowsAsync<ArgumentException>(async () => await cmd.PrepareAsync());
#else
			await cmd.PrepareAsync();
#endif
		}

		if (procedureType == "FUNCTION")
			await Assert.ThrowsAsync<InvalidOperationException>(async () => await cmd.ExecuteNonQueryAsync());
		else
			await Assert.ThrowsAsync<ArgumentException>(async () => await cmd.ExecuteNonQueryAsync());
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task StoredProcedureNoResultSet(bool prepare)
	{
		using var connection = CreateOpenConnection();
		using var cmd = connection.CreateCommand();
		cmd.CommandText = "out_string";
		cmd.CommandType = CommandType.StoredProcedure;
		cmd.Parameters.Add(new()
		{
			ParameterName = "@value",
			DbType = DbType.String,
			Direction = ParameterDirection.Output,
		});

		if (prepare)
			await cmd.PrepareAsync();
		using (var reader = await cmd.ExecuteReaderAsync())
		{
			Assert.False(await reader.ReadAsync());
			Assert.False(await reader.NextResultAsync());
		}

		Assert.Equal("test value", cmd.Parameters[0].Value);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task FieldCountForNoResultSet(bool prepare)
	{
		using var connection = CreateOpenConnection();
		using var cmd = connection.CreateCommand();
		cmd.CommandText = "out_string";
		cmd.CommandType = CommandType.StoredProcedure;
		cmd.Parameters.Add(new()
		{
			ParameterName = "@value",
			DbType = DbType.String,
			Direction = ParameterDirection.Output,
		});

		if (prepare)
			await cmd.PrepareAsync();
		using (var reader = await cmd.ExecuteReaderAsync())
		{
			Assert.Equal(0, reader.FieldCount);
			Assert.False(reader.HasRows);
			Assert.False(await reader.ReadAsync());
			Assert.Equal(0, reader.FieldCount);
			Assert.False(reader.HasRows);
		}

		Assert.Equal("test value", cmd.Parameters[0].Value);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetSchemaTableForNoResultSet(bool prepare)
	{
		using var connection = CreateOpenConnection();
		using var cmd = connection.CreateCommand();
		cmd.CommandText = "out_string";
		cmd.CommandType = CommandType.StoredProcedure;
		cmd.Parameters.Add(new()
		{
			ParameterName = "@value",
			DbType = DbType.String,
			Direction = ParameterDirection.Output,
		});

		if (prepare)
			await cmd.PrepareAsync();
		using var reader = await cmd.ExecuteReaderAsync();
		Assert.False(await reader.ReadAsync());
		var table = reader.GetSchemaTable();
		Assert.Null(table);
		Assert.False(await reader.NextResultAsync());
	}

#if !MYSQL_DATA
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetColumnSchemaForNoResultSet(bool prepare)
	{
		using var connection = CreateOpenConnection();
		using var cmd = connection.CreateCommand();
		cmd.CommandText = "out_string";
		cmd.CommandType = CommandType.StoredProcedure;
		cmd.Parameters.Add(new()
		{
			ParameterName = "@value",
			DbType = DbType.String,
			Direction = ParameterDirection.Output,
		});

		if (prepare)
			await cmd.PrepareAsync();
		using var reader = await cmd.ExecuteReaderAsync();
		Assert.False(await reader.ReadAsync());
		Assert.Empty(reader.GetColumnSchema());
		Assert.False(await reader.NextResultAsync());
	}
#endif

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task StoredProcedureOutIncorrectType(bool prepare)
	{
		using var connection = CreateOpenConnection();
		using var cmd = connection.CreateCommand();
		cmd.CommandText = "out_string";
		cmd.CommandType = CommandType.StoredProcedure;
		cmd.Parameters.Add(new()
		{
			ParameterName = "@value",
			DbType = DbType.Double,
			Direction = ParameterDirection.Output,
		});

		if (prepare)
			await cmd.PrepareAsync();
		await Assert.ThrowsAsync<FormatException>(async () => await cmd.ExecuteNonQueryAsync());
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task StoredProcedureReturnsNull(bool prepare)
	{
		using var connection = CreateOpenConnection();
		using var cmd = connection.CreateCommand();
		cmd.CommandText = "out_null";
		cmd.CommandType = CommandType.StoredProcedure;
		cmd.Parameters.Add(new()
		{
			ParameterName = "@string_value",
			DbType = DbType.String,
			Direction = ParameterDirection.Output,
			IsNullable = true,
			Value = "non null",
		});
		cmd.Parameters.Add(new()
		{
			ParameterName = "@int_value",
			DbType = DbType.Int32,
			Direction = ParameterDirection.Output,
			IsNullable = true,
			Value = "123",
		});

		if (prepare)
			await cmd.PrepareAsync();
		await cmd.ExecuteNonQueryAsync();

		Assert.Equal(DBNull.Value, cmd.Parameters["@string_value"].Value);
		Assert.Equal(DBNull.Value, cmd.Parameters["@int_value"].Value);
	}

	[Theory]
	[InlineData("NonQuery", true)]
	[InlineData("NonQuery", false)]
	[InlineData("Scalar", true)]
	[InlineData("Scalar", false)]
	[InlineData("Reader", true)]
	[InlineData("Reader", false)]
	public async Task StoredProcedureCircle(string executorType, bool prepare)
	{
		using var connection = CreateOpenConnection();
		using var cmd = connection.CreateCommand();
		cmd.CommandText = "circle";
		cmd.CommandType = CommandType.StoredProcedure;
		cmd.Parameters.Add(new()
		{
			ParameterName = "@radius",
			DbType = DbType.Double,
			Direction = ParameterDirection.Input,
			Value = 1.0,
		});
		cmd.Parameters.Add(new()
		{
			ParameterName = "@height",
			DbType = DbType.Double,
			Direction = ParameterDirection.Input,
			Value = 2.0,
		});
		cmd.Parameters.Add(new()
		{
			ParameterName = "@name",
			DbType = DbType.String,
			Direction = ParameterDirection.Input,
			Value = "awesome",
		});
		cmd.Parameters.Add(new()
		{
			ParameterName = "@diameter",
			DbType = DbType.Double,
			Direction = ParameterDirection.Output,
		});
		cmd.Parameters.Add(new()
		{
			ParameterName = "@circumference",
			DbType = DbType.Double,
			Direction = ParameterDirection.Output,
		});
		cmd.Parameters.Add(new()
		{
			ParameterName = "@area",
			DbType = DbType.Double,
			Direction = ParameterDirection.Output,
		});
		cmd.Parameters.Add(new()
		{
			ParameterName = "@volume",
			DbType = DbType.Double,
			Direction = ParameterDirection.Output,
		});
		cmd.Parameters.Add(new()
		{
			ParameterName = "@shape",
			DbType = DbType.String,
			Direction = ParameterDirection.Output,
		});

		if (prepare)
			await cmd.PrepareAsync();
		await CircleAssertions(cmd, executorType);
	}

	[SkippableTheory(ServerFeatures.StoredProcedures)]
	[InlineData("NonQuery", true)]
	[InlineData("NonQuery", false)]
	[InlineData("Scalar", true)]
	[InlineData("Scalar", false)]
	[InlineData("Reader", true)]
	[InlineData("Reader", false)]
	public async Task StoredProcedureCircleCached(string executorType, bool prepare)
	{
		// reorder parameters
		// remove return types
		// remove directions (MySqlConnector only, MySql.Data does not fix these up)
		// CachedProcedure class should fix everything up based on parameter names
		using var connection = CreateOpenConnection();
		using var cmd = connection.CreateCommand();
		cmd.CommandText = "circle";
		cmd.CommandType = CommandType.StoredProcedure;
		cmd.Parameters.Add(new()
		{
			ParameterName = "@name",
			Value = "awesome",
#if MYSQL_DATA
			Direction = ParameterDirection.Input,
#endif
		});
		cmd.Parameters.Add(new()
		{
			ParameterName = "@radius",
			Value = 1.5,
#if MYSQL_DATA
			Direction = ParameterDirection.Input,
#endif
		});
		cmd.Parameters.Add(new()
		{
			ParameterName = "@shape",
#if MYSQL_DATA
			Direction = ParameterDirection.Output,
#endif
		});
		cmd.Parameters.Add(new()
		{
			ParameterName = "@height",
			Value = 2.0,
#if MYSQL_DATA
			Direction = ParameterDirection.Input,
#endif
		});
		cmd.Parameters.Add(new()
		{
			ParameterName = "@diameter",
#if MYSQL_DATA
			Direction = ParameterDirection.Output,
#endif
		});
		cmd.Parameters.Add(new()
		{
			ParameterName = "@area",
#if MYSQL_DATA
			Direction = ParameterDirection.Output,
#endif
		});
		cmd.Parameters.Add(new()
		{
			ParameterName = "@volume",
#if MYSQL_DATA
			Direction = ParameterDirection.Output,
#endif
		});
		cmd.Parameters.Add(new()
		{
			ParameterName = "@circumference",
#if MYSQL_DATA
			Direction = ParameterDirection.Output,
#endif
		});

		if (prepare)
			await cmd.PrepareAsync();
		await CircleAssertions(cmd, executorType);
	}

	private async Task CircleAssertions(DbCommand cmd, string executorType)
	{
		var result = await ExecuteCommandAsync(cmd, executorType);
		if (executorType != "NonQuery")
			Assert.Equal((string) cmd.Parameters["@name"].Value + (string) cmd.Parameters["@shape"].Value, result);

		Assert.Equal(2 * (double) cmd.Parameters["@radius"].Value, cmd.Parameters["@diameter"].Value);
		Assert.Equal(2.0 * Math.PI * (double) cmd.Parameters["@radius"].Value, cmd.Parameters["@circumference"].Value);
		Assert.Equal(Math.PI * Math.Pow((double) cmd.Parameters["@radius"].Value, 2), cmd.Parameters["@area"].Value);
		Assert.Equal((double) cmd.Parameters["@area"].Value * (double) cmd.Parameters["@height"].Value, cmd.Parameters["@volume"].Value);
	}

	private async Task<object> ExecuteCommandAsync(DbCommand cmd, string executorType)
	{
		switch (executorType)
		{
			case "NonQuery":
				await cmd.ExecuteNonQueryAsync();
				return null;
			case "Scalar":
				return await cmd.ExecuteScalarAsync();
			default:
				using (var reader = await cmd.ExecuteReaderAsync())
				{
					if (await reader.ReadAsync())
						return reader.GetValue(0);
					return null;
				}
		}
	}

	[Theory]
	[InlineData("factor", true)]
	[InlineData("factor", false)]
	[InlineData("@factor", true)]
	[InlineData("@factor", false)]
	[InlineData("?factor", true)]
	[InlineData("?factor", false)]
	public async Task MultipleRows(string paramaterName, bool prepare)
	{
		using var connection = CreateOpenConnection();
		using var cmd = connection.CreateCommand();
		cmd.CommandText = "number_multiples";
		cmd.CommandType = CommandType.StoredProcedure;
		cmd.Parameters.Add(new() { ParameterName = paramaterName, Value = 3 });

		if (prepare)
			await cmd.PrepareAsync();
		using var reader = await cmd.ExecuteReaderAsync();
		Assert.True(await reader.ReadAsync());
		Assert.Equal("six", reader.GetString(0));
		Assert.True(await reader.ReadAsync());
		Assert.Equal("three", reader.GetString(0));
		Assert.False(await reader.ReadAsync());
		Assert.False(await reader.NextResultAsync());
	}

	[Theory]
	[InlineData(1, new string[0], new[] { "eight", "five", "four", "seven", "six", "three", "two" }, true)]
	[InlineData(1, new string[0], new[] { "eight", "five", "four", "seven", "six", "three", "two" }, false)]
	[InlineData(4, new[] { "one", "three", "two" }, new[] { "eight", "five", "seven", "six" }, true)]
	[InlineData(4, new[] { "one", "three", "two" }, new[] { "eight", "five", "seven", "six" }, false)]
	[InlineData(8, new[] { "five", "four", "one", "seven", "six", "three", "two" }, new string[0], true)]
	[InlineData(8, new[] { "five", "four", "one", "seven", "six", "three", "two" }, new string[0], false)]
	public async Task MultipleResultSets(int pivot, string[] firstResultSet, string[] secondResultSet, bool prepare)
	{
		using var connection = CreateOpenConnection();
		using var cmd = connection.CreateCommand();
		cmd.CommandText = "multiple_result_sets";
		cmd.CommandType = CommandType.StoredProcedure;
		cmd.Parameters.Add(new() { ParameterName = "@pivot", Value = pivot });

		if (prepare)
			await cmd.PrepareAsync();
		using var reader = await cmd.ExecuteReaderAsync();
		foreach (var result in firstResultSet)
		{
			Assert.True(await reader.ReadAsync());
			Assert.Equal(result, reader.GetString(0));
		}
		Assert.False(await reader.ReadAsync());

		Assert.True(await reader.NextResultAsync());

		foreach (var result in secondResultSet)
		{
			Assert.True(await reader.ReadAsync());
			Assert.Equal(result, reader.GetString(0));
		}
		Assert.False(await reader.ReadAsync());

		Assert.False(await reader.NextResultAsync());
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task InOut(bool prepare)
	{
		using var connection = CreateOpenConnection();
		var parameter = new MySqlParameter
		{
			ParameterName = "high",
			DbType = DbType.Int32,
			Direction = ParameterDirection.InputOutput,
			Value = 1,
		};
		while ((int) parameter.Value < 8)
		{
			using var cmd = connection.CreateCommand();
			var nextValue = (int) parameter.Value + 1;
			cmd.CommandText = "number_lister";
			cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.Add(parameter);
			if (prepare)
				await cmd.PrepareAsync();
			using (var reader = await cmd.ExecuteReaderAsync())
			{
				for (var i = 0; i < (int) parameter.Value; i++)
				{
					Assert.True(await reader.ReadAsync());
					Assert.Equal(i + 1, reader.GetInt32(0));
					Assert.True(reader.GetString(1).Length > 0);
				}
				await reader.NextResultAsync();
			}
			Assert.Equal(nextValue, parameter.Value);
		}
	}

	[Theory]
	[InlineData(false, true)]
	[InlineData(false, false)]
	[InlineData(true, true)]
	[InlineData(true, false)]
	public async Task DottedName(bool useDatabaseName, bool prepare)
	{
		using var connection = CreateOpenConnection();
		using var cmd = connection.CreateCommand();
		cmd.CommandText = (useDatabaseName ? $"{connection.Database}." : "") + "`dotted.name`";
		cmd.CommandType = CommandType.StoredProcedure;

		if (prepare)
			await cmd.PrepareAsync();
		using var reader = await cmd.ExecuteReaderAsync();
		Assert.True(await reader.ReadAsync());
		Assert.Equal(1, reader.GetInt32(0));
		Assert.Equal(2, reader.GetInt32(1));
		Assert.Equal(3, reader.GetInt32(2));
		Assert.False(await reader.ReadAsync());
		Assert.False(await reader.NextResultAsync());
	}

	[Fact]
	public void DeriveParametersCircle()
	{
		using var cmd = new MySqlCommand("circle", m_database.Connection);
		cmd.CommandType = CommandType.StoredProcedure;
		MySqlCommandBuilder.DeriveParameters(cmd);

		Assert.Collection(cmd.Parameters.Cast<MySqlParameter>(),
			AssertParameter("@radius", ParameterDirection.Input, MySqlDbType.Double),
			AssertParameter("@height", ParameterDirection.Input, MySqlDbType.Double),
			AssertParameter("@name", ParameterDirection.Input, MySqlDbType.VarChar),
			AssertParameter("@diameter", ParameterDirection.Output, MySqlDbType.Double),
			AssertParameter("@circumference", ParameterDirection.Output, MySqlDbType.Double),
			AssertParameter("@area", ParameterDirection.Output, MySqlDbType.Double),
			AssertParameter("@volume", ParameterDirection.Output, MySqlDbType.Double),
			AssertParameter("@shape", ParameterDirection.Output, MySqlDbType.VarChar));
	}

	[Fact]
	public void DeriveParametersNumberLister()
	{
		using var cmd = new MySqlCommand("number_lister", m_database.Connection);
		cmd.CommandType = CommandType.StoredProcedure;
		MySqlCommandBuilder.DeriveParameters(cmd);

		Assert.Collection(cmd.Parameters.Cast<MySqlParameter>(),
			AssertParameter("@high", ParameterDirection.InputOutput, MySqlDbType.Int32));
	}

	[Fact]
	public void DeriveParametersRemovesExisting()
	{
		using var cmd = new MySqlCommand("number_lister", m_database.Connection);
		cmd.CommandType = CommandType.StoredProcedure;
		cmd.Parameters.AddWithValue("test1", 1);
		cmd.Parameters.AddWithValue("test2", 2);
		cmd.Parameters.AddWithValue("test3", 3);

		MySqlCommandBuilder.DeriveParameters(cmd);
		Assert.Collection(cmd.Parameters.Cast<MySqlParameter>(),
			AssertParameter("@high", ParameterDirection.InputOutput, MySqlDbType.Int32));
	}

	[Fact]
	public void DeriveParametersDoesNotExist()
	{
		using var cmd = new MySqlCommand("xx_does_not_exist", m_database.Connection);
		cmd.CommandType = CommandType.StoredProcedure;
		Assert.Throws<MySqlException>(() => MySqlCommandBuilder.DeriveParameters(cmd));
	}

	[Fact]
	public void DeriveParametersDoesNotExistThenIsCreated()
	{
		using (var cmd = new MySqlCommand("drop procedure if exists xx_does_not_exist_2;", m_database.Connection))
			cmd.ExecuteNonQuery();

		using (var cmd = new MySqlCommand("xx_does_not_exist_2", m_database.Connection))
		{
			cmd.CommandType = CommandType.StoredProcedure;
			Assert.Throws<MySqlException>(() => MySqlCommandBuilder.DeriveParameters(cmd));
		}

		using (var cmd = new MySqlCommand(@"create procedure xx_does_not_exist_2(
				IN param1 INT,
				OUT param2 VARCHAR(100))
			BEGIN
				SELECT 'test' INTO param2;
			END", m_database.Connection))
		{
			cmd.ExecuteNonQuery();
		}

		using (var cmd = new MySqlCommand("xx_does_not_exist_2", m_database.Connection))
		{
			cmd.CommandType = CommandType.StoredProcedure;
			MySqlCommandBuilder.DeriveParameters(cmd);
			Assert.Collection(cmd.Parameters.Cast<MySqlParameter>(),
				AssertParameter("@param1", ParameterDirection.Input, MySqlDbType.Int32),
				AssertParameter("@param2", ParameterDirection.Output, MySqlDbType.VarChar));
		}
	}

	[Theory]
#if !MYSQL_DATA
	[InlineData("bool", 1)]
	[InlineData("tinyint(1)", 1)]
	[InlineData("decimal(10)", 10)]

	// https://bugs.mysql.com/bug.php?id=112088
	[InlineData("bit(1)", 1)]
	[InlineData("bit(10)", 10)]
	[InlineData("mediumtext", 0)]
#endif
	[InlineData("char(30)", 30)]
	[InlineData("varchar(50)", 50)]
	//// These return nonzero sizes for some versions of MySQL Server 8.0
	//// [InlineData("bit", 0)]
	//// [InlineData("tinyint", 0)]
	//// [InlineData("bigint", 0)]
	//// [InlineData("bigint unsigned", 0)]
	public void DeriveParametersParameterSize(string parameterType, int expectedSize)
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.Pooling = false;
		using var connection = new MySqlConnection(csb.ConnectionString);
		connection.Open();

		using (var cmd = new MySqlCommand($"drop procedure if exists parameter_size; create procedure parameter_size(in param1 {parameterType}) begin end;", connection))
			cmd.ExecuteNonQuery();

		using (var cmd = new MySqlCommand("parameter_size", connection))
		{
			cmd.CommandType = CommandType.StoredProcedure;
			MySqlCommandBuilder.DeriveParameters(cmd);
			var parameter = (MySqlParameter) Assert.Single(cmd.Parameters);
			Assert.Equal(expectedSize, parameter.Size);
		}
	}

	[Theory]
	[InlineData("bit", MySqlDbType.Bit)]
	[InlineData("bit(1)", MySqlDbType.Bit)]
#if MYSQL_DATA
	[InlineData("bool", MySqlDbType.Byte)]
	[InlineData("tinyint(1)", MySqlDbType.Byte)]
#else
	[InlineData("bool", MySqlDbType.Bool)]
	[InlineData("tinyint(1)", MySqlDbType.Bool)]
#endif
	[InlineData("tinyint", MySqlDbType.Byte)]
	[InlineData("bigint", MySqlDbType.Int64)]
	[InlineData("bigint unsigned", MySqlDbType.UInt64)]
	[InlineData("char(30)", MySqlDbType.String)]
	[InlineData("mediumtext", MySqlDbType.MediumText)]
	[InlineData("varchar(50)", MySqlDbType.VarChar)]
	[InlineData("decimal(10, 0)", MySqlDbType.NewDecimal)]
	[InlineData("decimal(10, 0) unsigned", MySqlDbType.NewDecimal)]
	public void DeriveParametersParameterType(string parameterType, MySqlDbType expectedType)
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.Pooling = false;
		using var connection = new MySqlConnection(csb.ConnectionString);
		connection.Open();

		using (var cmd = new MySqlCommand($"drop procedure if exists parameter_size; create procedure parameter_size(in param1 {parameterType}) begin end;", connection))
			cmd.ExecuteNonQuery();

		using (var cmd = new MySqlCommand("parameter_size", connection))
		{
			cmd.CommandType = CommandType.StoredProcedure;
			MySqlCommandBuilder.DeriveParameters(cmd);
			var parameter = (MySqlParameter) Assert.Single(cmd.Parameters);
			Assert.Equal(expectedType, parameter.MySqlDbType);
		}
	}

	[SkippableFact(ServerFeatures.Json, MySqlData = "https://bugs.mysql.com/bug.php?id=89335")]
	public void DeriveParametersSetJson()
	{
		using var cmd = new MySqlCommand("SetJson", m_database.Connection);
		cmd.CommandType = CommandType.StoredProcedure;
		MySqlCommandBuilder.DeriveParameters(cmd);

		Assert.Collection(cmd.Parameters.Cast<MySqlParameter>(),
			AssertParameter("@vJson", ParameterDirection.Input, MySqlDbType.JSON));
	}

	[SkippableFact(ServerFeatures.Json)]
	public void PassJsonParameter()
	{
		using var cmd = new MySqlCommand("SetJson", m_database.Connection);
		cmd.CommandType = CommandType.StoredProcedure;
		var json = "{\"prop\":[null]}";
		cmd.Parameters.AddWithValue("@vJson", json).MySqlDbType = MySqlDbType.JSON;
		using var reader = cmd.ExecuteReader();
		Assert.True(reader.Read());
		Assert.Equal(json, reader.GetString(0).Replace(" ", ""));
		Assert.False(reader.Read());
	}

	private static Action<MySqlParameter> AssertParameter(string name, ParameterDirection direction, MySqlDbType mySqlDbType)
	{
		return x =>
		{
			Assert.Equal(name, x.ParameterName);
			Assert.Equal(direction, x.Direction);
			Assert.Equal(mySqlDbType, x.MySqlDbType);
		};
	}

	[Theory]
	[InlineData("echof", "FUNCTION", "varchar(63)", "BEGIN RETURN name; END", "NO", "CONTAINS SQL")]
	[InlineData("echop", "PROCEDURE", null, "BEGIN SELECT name; END", "NO", "CONTAINS SQL")]
	[InlineData("failing_function", "FUNCTION", "decimal(10,5)", "BEGIN DECLARE v1 DECIMAL(10,5); SELECT c1 FROM table_that_does_not_exist INTO v1; RETURN v1; END", "NO", "CONTAINS SQL")]
	public void ProceduresSchema(string procedureName, string procedureType, string dtdIdentifier, string routineDefinition, string isDeterministic, string dataAccess)
	{
		var dataTable = m_database.Connection.GetSchema("Procedures");
		var schema = m_database.Connection.Database;
		var row = dataTable.Rows.Cast<DataRow>().Single(x => schema.Equals(x["ROUTINE_SCHEMA"]) && procedureName.Equals(x["ROUTINE_NAME"]));

		Assert.Equal(procedureName, row["SPECIFIC_NAME"]);
		Assert.Equal(procedureType, row["ROUTINE_TYPE"]);
		if (dtdIdentifier is null)
			Assert.Equal(DBNull.Value, row["DTD_IDENTIFIER"]);
		else
			Assert.Equal(dtdIdentifier, ((string) row["DTD_IDENTIFIER"]).Split(' ')[0]);
		Assert.Equal(routineDefinition, NormalizeSpaces((string) row["ROUTINE_DEFINITION"]));
		Assert.Equal(isDeterministic, row["IS_DETERMINISTIC"]);
		Assert.Equal(dataAccess, ((string) row["SQL_DATA_ACCESS"]).Replace('_', ' '));
	}

	[Fact]
	public void CallNonExistentStoredProcedure()
	{
		using var command = new MySqlCommand("NonExistentStoredProcedure", m_database.Connection);
		command.CommandType = CommandType.StoredProcedure;
		Assert.Throws<MySqlException>(() => command.ExecuteNonQuery());
	}

	[Fact]
	public void PrepareNonExistentStoredProcedure()
	{
		using var connection = CreateOpenConnection();
		using var command = new MySqlCommand("NonExistentStoredProcedure", connection);
		command.CommandType = CommandType.StoredProcedure;
		Assert.Throws<MySqlException>(command.Prepare);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void OutputTimeParameter(bool prepare)
	{
		using var connection = CreateOpenConnection();
		using var command = new MySqlCommand("GetTime", connection);
		command.CommandType = CommandType.StoredProcedure;
		var parameter = command.CreateParameter();
		parameter.ParameterName = "OutTime";
		parameter.Direction = ParameterDirection.Output;
		command.Parameters.Add(parameter);

		if (prepare)
			command.Prepare();
		command.ExecuteNonQuery();
		Assert.IsType<TimeSpan>(parameter.Value);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void EnumProcedure(bool prepare)
	{
		using var connection = CreateOpenConnection();
		using var command = new MySqlCommand("EnumProcedure", connection);
		command.CommandType = CommandType.StoredProcedure;
		command.Parameters.AddWithValue("@input", "One");
		if (prepare)
			command.Prepare();
		using var reader = command.ExecuteReader();
		Assert.True(reader.Read());
		Assert.Equal("One", reader.GetString(0));
		Assert.False(reader.Read());
	}

	[Theory]
	[InlineData("`a b`")]
	[InlineData("`a.b`")]
	[InlineData("`a``b`")]
	[InlineData("`a b.c ``d`")]
	public void SprocNameSpecialCharacters(string sprocName)
	{
		using var connection = CreateOpenConnection();

		using (var command = new MySqlCommand($@"DROP PROCEDURE IF EXISTS {sprocName};
CREATE PROCEDURE {sprocName} ()
BEGIN
	SELECT 'test' AS Result;
END;", connection))
		{
			command.ExecuteNonQuery();
		}

		using (var command = new MySqlCommand(sprocName, connection))
		{
			command.CommandType = CommandType.StoredProcedure;

			using var reader = command.ExecuteReader();
			Assert.True(reader.Read());
			Assert.Equal("test", reader.GetString(0));
			Assert.False(reader.Read());
		}
	}

#if !MYSQL_DATA
	[Theory]
	[InlineData(MySqlGuidFormat.Binary16, "BINARY(16)", "X'BABD8384C908499C9D95C02ADA94A970'", false, false)]
	[InlineData(MySqlGuidFormat.Binary16, "BINARY(16)", "X'BABD8384C908499C9D95C02ADA94A970'", false, true)]
	[InlineData(MySqlGuidFormat.Binary16, "BINARY(16)", "X'BABD8384C908499C9D95C02ADA94A970'", true, false)]
	[InlineData(MySqlGuidFormat.Binary16, "BINARY(16)", "X'BABD8384C908499C9D95C02ADA94A970'", true, true)]
	[InlineData(MySqlGuidFormat.Char32, "CHAR(32)", "'BABD8384C908499C9D95C02ADA94A970'", false, false)]
	[InlineData(MySqlGuidFormat.Char32, "CHAR(32)", "'BABD8384C908499C9D95C02ADA94A970'", false, true)]
	[InlineData(MySqlGuidFormat.Char32, "CHAR(32)", "'BABD8384C908499C9D95C02ADA94A970'", true, false)]
	[InlineData(MySqlGuidFormat.Char32, "CHAR(32)", "'BABD8384C908499C9D95C02ADA94A970'", true, true)]
	[InlineData(MySqlGuidFormat.Char36, "CHAR(36)", "'BABD8384-C908-499C-9D95-C02ADA94A970'", false, false)]
	[InlineData(MySqlGuidFormat.Char36, "CHAR(36)", "'BABD8384-C908-499C-9D95-C02ADA94A970'", false, true)]
	[InlineData(MySqlGuidFormat.Char36, "CHAR(36)", "'BABD8384-C908-499C-9D95-C02ADA94A970'", true, false)]
	[InlineData(MySqlGuidFormat.Char36, "CHAR(36)", "'BABD8384-C908-499C-9D95-C02ADA94A970'", true, true)]
	public void StoredProcedureReturnsGuid(MySqlGuidFormat guidFormat, string columnDefinition, string columnValue, bool setMySqlDbType, bool prepare)
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.GuidFormat = guidFormat;
		csb.Pooling = false;
		using var connection = new MySqlConnection(csb.ConnectionString);
		connection.Open();

		using (var command = new MySqlCommand($"""
			DROP TABLE IF EXISTS out_guid_table;
			CREATE TABLE out_guid_table (id INT PRIMARY KEY AUTO_INCREMENT, guid {columnDefinition});
			INSERT INTO out_guid_table (guid) VALUES ({columnValue});
			DROP PROCEDURE IF EXISTS out_guid;
			CREATE PROCEDURE out_guid
			(
				OUT out_name {columnDefinition}
			)
			BEGIN
				SELECT guid INTO out_name FROM out_guid_table;
			END;
			""", connection))
		{
			command.ExecuteNonQuery();
		}

		using (var command = new MySqlCommand("out_guid", connection))
		{
			command.CommandType = CommandType.StoredProcedure;
			var param = new MySqlParameter("out_name", null) { Direction = ParameterDirection.Output };
			if (setMySqlDbType)
				param.MySqlDbType = MySqlDbType.Guid;
			command.Parameters.Add(param);
			command.ExecuteNonQuery();
			if (prepare)
				command.Prepare();
			Assert.Equal(new Guid("BABD8384C908499C9D95C02ADA94A970"), param.Value);
		}
	}
#endif

	private static string NormalizeSpaces(string input)
	{
		input = input.Replace('\r', ' ');
		input = input.Replace('\n', ' ');
		input = input.Replace('\t', ' ');
		int startingLength;
		do
		{
			startingLength = input.Length;
			input = input.Replace("  ", " ");
		} while (input.Length != startingLength);
		return input;
	}

	private static MySqlConnection CreateOpenConnection()
	{
		var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();
		return connection;
	}

	private readonly DatabaseFixture m_database;
}
