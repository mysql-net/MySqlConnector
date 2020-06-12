using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Xunit;

namespace SideBySide
{
	public class StoredProcedureTests : IClassFixture<StoredProcedureFixture>
	{
		public StoredProcedureTests(StoredProcedureFixture database)
		{
			m_database = database;
		}

		[Theory]
		[InlineData("FUNCTION", "NonQuery", true)]
		[InlineData("FUNCTION", "Scalar", true)]
		[InlineData("FUNCTION", "Reader", true)]
		[InlineData("PROCEDURE", "NonQuery", true)]
		[InlineData("PROCEDURE", "NonQuery", false)]
		[InlineData("PROCEDURE", "Scalar", true)]
		[InlineData("PROCEDURE", "Scalar", false)]
		[InlineData("PROCEDURE", "Reader", true)]
		[InlineData("PROCEDURE", "Reader", false)]
		public async Task StoredProcedureEcho(string procedureType, string executorType, bool ignorePrepare)
		{
			using var connection = CreateOpenConnection(ignorePrepare);
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

			command.Prepare();
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

			command.Prepare();
			Assert.Throws<MySqlException>(() => command.ExecuteNonQuery());
			transaction.Commit();
		}

		[SkippableTheory(ServerFeatures.StoredProcedures)]
		[InlineData("FUNCTION", true)]
		[InlineData("FUNCTION", false)]
		[InlineData("PROCEDURE", true)]
		[InlineData("PROCEDURE", false)]
		public async Task StoredProcedureEchoException(string procedureType, bool ignorePrepare)
		{
			using var connection = CreateOpenConnection(ignorePrepare);
			using var cmd = connection.CreateCommand();
			cmd.CommandText = "echo" + (procedureType == "FUNCTION" ? "f" : "p");
			cmd.CommandType = CommandType.StoredProcedure;

			if (procedureType == "FUNCTION")
				await Assert.ThrowsAsync<InvalidOperationException>(async () => await cmd.ExecuteNonQueryAsync());
			else
				await Assert.ThrowsAsync<ArgumentException>(async () => await cmd.ExecuteNonQueryAsync());
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task StoredProcedureNoResultSet(bool ignorePrepare)
		{
			using var connection = CreateOpenConnection(ignorePrepare);
			using var cmd = connection.CreateCommand();
			cmd.CommandText = "out_string";
			cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.Add(new()
			{
				ParameterName = "@value",
				DbType = DbType.String,
				Direction = ParameterDirection.Output,
			});

			await cmd.PrepareAsync();
			using (var reader = await cmd.ExecuteReaderAsync())
			{
				Assert.False(await reader.ReadAsync());
				Assert.False(await reader.NextResultAsync());
			}

			Assert.Equal("test value", cmd.Parameters[0].Value);
		}

		[SkippableTheory(Baseline = "https://bugs.mysql.com/bug.php?id=97300")]
		[InlineData(true)]
		[InlineData(false)]
		public async Task FieldCountForNoResultSet(bool ignorePrepare)
		{
			using var connection = CreateOpenConnection(ignorePrepare);
			using var cmd = connection.CreateCommand();
			cmd.CommandText = "out_string";
			cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.Add(new()
			{
				ParameterName = "@value",
				DbType = DbType.String,
				Direction = ParameterDirection.Output,
			});

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

#if !NETCOREAPP1_1_2
		[SkippableTheory(Baseline = "https://bugs.mysql.com/bug.php?id=97300")]
		[InlineData(true)]
		[InlineData(false)]
		public async Task GetSchemaTableForNoResultSet(bool ignorePrepare)
		{
			using var connection = CreateOpenConnection(ignorePrepare);
			using var cmd = connection.CreateCommand();
			cmd.CommandText = "out_string";
			cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.Add(new()
			{
				ParameterName = "@value",
				DbType = DbType.String,
				Direction = ParameterDirection.Output,
			});

			await cmd.PrepareAsync();
			using var reader = await cmd.ExecuteReaderAsync();
			Assert.False(await reader.ReadAsync());
			var table = reader.GetSchemaTable();
			Assert.NotNull(table);
			Assert.Empty(table.Rows);
			Assert.Empty(table.Columns);
			Assert.False(await reader.NextResultAsync());
		}
#endif

#if !BASELINE
		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task GetColumnSchemaForNoResultSet(bool ignorePrepare)
		{
			using var connection = CreateOpenConnection(ignorePrepare);
			using var cmd = connection.CreateCommand();
			cmd.CommandText = "out_string";
			cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.Add(new()
			{
				ParameterName = "@value",
				DbType = DbType.String,
				Direction = ParameterDirection.Output,
			});

			await cmd.PrepareAsync();
			using var reader = await cmd.ExecuteReaderAsync();
			Assert.False(await reader.ReadAsync());
			Assert.Empty(reader.GetColumnSchema());
			Assert.False(await reader.NextResultAsync());
		}
#endif

		[Theory]
		[InlineData(true)]
#if !BASELINE
		[InlineData(false)] // https://bugs.mysql.com/bug.php?id=99793
#endif
		public async Task StoredProcedureOutIncorrectType(bool ignorePrepare)
		{
			using var connection = CreateOpenConnection(ignorePrepare);
			using var cmd = connection.CreateCommand();
			cmd.CommandText = "out_string";
			cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.Add(new()
			{
				ParameterName = "@value",
				DbType = DbType.Double,
				Direction = ParameterDirection.Output,
			});

			await cmd.PrepareAsync();
			await Assert.ThrowsAsync<FormatException>(cmd.ExecuteNonQueryAsync);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task StoredProcedureReturnsNull(bool ignorePrepare)
		{
			using var connection = CreateOpenConnection(ignorePrepare);
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
		public async Task StoredProcedureCircle(string executorType, bool ignorePrepare)
		{
			using var connection = CreateOpenConnection(ignorePrepare);
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
		public async Task StoredProcedureCircleCached(string executorType, bool ignorePrepare)
		{
			// reorder parameters
			// remove return types
			// remove directions (MySqlConnector only, MySql.Data does not fix these up)
			// CachedProcedure class should fix everything up based on parameter names
			using var connection = CreateOpenConnection(ignorePrepare);
			using var cmd = connection.CreateCommand();
			cmd.CommandText = "circle";
			cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.Add(new()
			{
				ParameterName = "@name",
				Value = "awesome",
#if BASELINE
				Direction = ParameterDirection.Input,
#endif
			});
			cmd.Parameters.Add(new()
			{
				ParameterName = "@radius",
				Value = 1.5,
#if BASELINE
				Direction = ParameterDirection.Input,
#endif
			});
			cmd.Parameters.Add(new()
			{
				ParameterName = "@shape",
#if BASELINE
				Direction = ParameterDirection.Output,
#endif
			});
			cmd.Parameters.Add(new()
			{
				ParameterName = "@height",
				Value = 2.0,
#if BASELINE
				Direction = ParameterDirection.Input,
#endif
			});
			cmd.Parameters.Add(new()
			{
				ParameterName = "@diameter",
#if BASELINE
				Direction = ParameterDirection.Output,
#endif
			});
			cmd.Parameters.Add(new()
			{
				ParameterName = "@area",
#if BASELINE
				Direction = ParameterDirection.Output,
#endif
			});
			cmd.Parameters.Add(new()
			{
				ParameterName = "@volume",
#if BASELINE
				Direction = ParameterDirection.Output,
#endif
			});
			cmd.Parameters.Add(new()
			{
				ParameterName = "@circumference",
#if BASELINE
				Direction = ParameterDirection.Output,
#endif
			});

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
		public async Task MultipleRows(string paramaterName, bool ignorePrepare)
		{
			using var connection = CreateOpenConnection(ignorePrepare);
			using var cmd = connection.CreateCommand();
			cmd.CommandText = "number_multiples";
			cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.Add(new() { ParameterName = paramaterName, Value = 3 });

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
		public async Task MultipleResultSets(int pivot, string[] firstResultSet, string[] secondResultSet, bool ignorePrepare)
		{
			using var connection = CreateOpenConnection(ignorePrepare);
			using var cmd = connection.CreateCommand();
			cmd.CommandText = "multiple_result_sets";
			cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.Add(new() { ParameterName = "@pivot", Value = pivot });

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
		public async Task InOut(bool ignorePrepare)
		{
			using var connection = CreateOpenConnection(ignorePrepare);
			var parameter = new MySqlParameter
			{
				ParameterName = "high",
				DbType = DbType.Int32,
				Direction = ParameterDirection.InputOutput,
				Value = 1
			};
			while ((int) parameter.Value < 8)
			{
				using var cmd = connection.CreateCommand();
				var nextValue = (int) parameter.Value + 1;
				cmd.CommandText = "number_lister";
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.Parameters.Add(parameter);
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

		[SkippableTheory(Baseline = "https://bugs.mysql.com/bug.php?id=84220")]
		[InlineData(false, true)]
		[InlineData(false, false)]
		[InlineData(true, true)]
		[InlineData(true, false)]
		public async Task DottedName(bool useDatabaseName, bool ignorePrepare)
		{
			using var connection = CreateOpenConnection(ignorePrepare);
			using var cmd = connection.CreateCommand();
			cmd.CommandText = (useDatabaseName ? $"{connection.Database}." : "") + "`dotted.name`";
			cmd.CommandType = CommandType.StoredProcedure;

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

		[SkippableFact(ServerFeatures.Json, Baseline = "https://bugs.mysql.com/bug.php?id=89335")]
		public void DeriveParametersSetJson()
		{
			using var cmd = new MySqlCommand("SetJson", m_database.Connection);
			cmd.CommandType = CommandType.StoredProcedure;
			MySqlCommandBuilder.DeriveParameters(cmd);

			Assert.Collection(cmd.Parameters.Cast<MySqlParameter>(),
				AssertParameter("@vJson", ParameterDirection.Input, MySqlDbType.JSON));
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

#if !NETCOREAPP1_1_2
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
#endif

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
			using var connection = CreateOpenConnection(ignorePrepare: false);
			using var command = new MySqlCommand("NonExistentStoredProcedure", connection);
			command.CommandType = CommandType.StoredProcedure;
			Assert.Throws<MySqlException>(command.Prepare);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void OutputTimeParameter(bool ignorePrepare)
		{
			using var connection = CreateOpenConnection(ignorePrepare);
			using var command = new MySqlCommand("GetTime", connection);
			command.CommandType = CommandType.StoredProcedure;
			var parameter = command.CreateParameter();
			parameter.ParameterName = "OutTime";
			parameter.Direction = ParameterDirection.Output;
			command.Parameters.Add(parameter);

			command.Prepare();
			command.ExecuteNonQuery();
			Assert.IsType<TimeSpan>(parameter.Value);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void EnumProcedure(bool ignorePrepare)
		{
			using var connection = CreateOpenConnection(ignorePrepare);
			using var command = new MySqlCommand("EnumProcedure", connection);
			command.CommandType = CommandType.StoredProcedure;
			command.Parameters.AddWithValue("@input", "One");
			command.Prepare();
			using var reader = command.ExecuteReader();
			Assert.True(reader.Read());
			Assert.Equal("One", reader.GetString(0));
			Assert.False(reader.Read());
		}

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

		private static MySqlConnection CreateOpenConnection(bool ignorePrepare)
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			csb.IgnorePrepare = ignorePrepare;
			var connection = new MySqlConnection(csb.ConnectionString);
			connection.Open();
			return connection;
		}

		readonly DatabaseFixture m_database;
	}
}
