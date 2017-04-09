using System;
using System.Data;
using System.Data.Common;
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
		[InlineData("FUNCTION", "NonQuery")]
		[InlineData("FUNCTION", "Scalar")]
		[InlineData("FUNCTION", "Reader")]
		[InlineData("PROCEDURE", "NonQuery")]
		[InlineData("PROCEDURE", "Scalar")]
		[InlineData("PROCEDURE", "Reader")]
		public async Task StoredProcedureEcho(string procedureType, string executorType)
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = "echo" + (procedureType == "FUNCTION" ? "f" : "p");
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.Add(new MySqlParameter
				{
					ParameterName = "@name",
					DbType = DbType.String,
					Direction = ParameterDirection.Input,
					Value = "hello",
				});

				// we make the assumption that Stored Procedures with ParameterDirection.ReturnValue are functions
				if (procedureType == "FUNCTION")
				{
					cmd.Parameters.Add(new MySqlParameter
					{
						ParameterName = "@result",
						DbType = DbType.String,
						Direction = ParameterDirection.ReturnValue,
					});
				}

				var result = await ExecuteCommandAsync(cmd, executorType);
				if (procedureType == "PROCEDURE" && executorType != "NonQuery")
					Assert.Equal(cmd.Parameters["@name"].Value, result);
				if (procedureType == "FUNCTION")
					Assert.Equal(cmd.Parameters["@name"].Value, cmd.Parameters["@result"].Value);
			}
		}

		[CachedProcedureTheory]
		[InlineData("FUNCTION")]
		[InlineData("PROCEDURE")]
		public async Task StoredProcedureEchoException(string procedureType)
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = "echo" + (procedureType == "FUNCTION" ? "f" : "p");
				cmd.CommandType = CommandType.StoredProcedure;

				if (procedureType == "FUNCTION")
					await Assert.ThrowsAsync(typeof(InvalidOperationException), async () => await cmd.ExecuteNonQueryAsync());
				else
					await Assert.ThrowsAsync(typeof(ArgumentException), async () => await cmd.ExecuteNonQueryAsync());
			}
		}

		[Theory]
		[InlineData("NonQuery")]
		[InlineData("Scalar")]
		[InlineData("Reader")]
		public async Task StoredProcedureCircle(string executorType)
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = "circle";
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.Parameters.Add(new MySqlParameter
				{
					ParameterName = "@radius",
					DbType = DbType.Double,
					Direction = ParameterDirection.Input,
					Value = 1.0,
				});
				cmd.Parameters.Add(new MySqlParameter
				{
					ParameterName = "@height",
					DbType = DbType.Double,
					Direction = ParameterDirection.Input,
					Value = 2.0,
				});
				cmd.Parameters.Add(new MySqlParameter
				{
					ParameterName = "@name",
					DbType = DbType.String,
					Direction = ParameterDirection.Input,
					Value = "awesome",
				});
				cmd.Parameters.Add(new MySqlParameter
				{
					ParameterName = "@diameter",
					DbType = DbType.Double,
					Direction = ParameterDirection.Output,
				});
				cmd.Parameters.Add(new MySqlParameter
				{
					ParameterName = "@circumference",
					DbType = DbType.Double,
					Direction = ParameterDirection.Output,
				});
				cmd.Parameters.Add(new MySqlParameter
				{
					ParameterName = "@area",
					DbType = DbType.Double,
					Direction = ParameterDirection.Output,
				});
				cmd.Parameters.Add(new MySqlParameter
				{
					ParameterName = "@volume",
					DbType = DbType.Double,
					Direction = ParameterDirection.Output,
				});
				cmd.Parameters.Add(new MySqlParameter
				{
					ParameterName = "@shape",
					DbType = DbType.String,
					Direction = ParameterDirection.Output,
				});

				await CircleAssertions(cmd, executorType);
			}
		}

		[CachedProcedureTheory]
		[InlineData("NonQuery")]
		[InlineData("Scalar")]
		[InlineData("Reader")]
		public async Task StoredProcedureCircleCached(string executorType)
		{
			// reorder parameters
			// remove return types
			// remove directions (MySqlConnector only, MySql.Data does not fix these up)
			// CachedProcedure class should fix everything up based on parameter names
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = "circle";
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.Parameters.Add(new MySqlParameter
				{
					ParameterName = "@name",
					Value = "awesome",
#if BASELINE
					Direction = ParameterDirection.Input,
#endif
				});
				cmd.Parameters.Add(new MySqlParameter
				{
					ParameterName = "@radius",
					Value = 1.0,
#if BASELINE
					Direction = ParameterDirection.Input,
#endif
				});
				cmd.Parameters.Add(new MySqlParameter
				{
					ParameterName = "@shape",
#if BASELINE
					Direction = ParameterDirection.Output,
#endif
				});
				cmd.Parameters.Add(new MySqlParameter
				{
					ParameterName = "@height",
					Value = 2.0,
#if BASELINE
					Direction = ParameterDirection.Input,
#endif
				});
				cmd.Parameters.Add(new MySqlParameter
				{
					ParameterName = "@diameter",
#if BASELINE
					Direction = ParameterDirection.Output,
#endif
				});
				cmd.Parameters.Add(new MySqlParameter
				{
					ParameterName = "@area",
#if BASELINE
					Direction = ParameterDirection.Output,
#endif
				});
				cmd.Parameters.Add(new MySqlParameter
				{
					ParameterName = "@volume",
#if BASELINE
					Direction = ParameterDirection.Output,
#endif
				});
				cmd.Parameters.Add(new MySqlParameter
				{
					ParameterName = "@circumference",
#if BASELINE
					Direction = ParameterDirection.Output,
#endif
				});

				await CircleAssertions(cmd, executorType);
			}
		}

		private async Task CircleAssertions(DbCommand cmd, string executorType)
		{
			var result = await ExecuteCommandAsync(cmd, executorType);
			if (executorType != "NonQuery")
				Assert.Equal((string)cmd.Parameters["@name"].Value + (string)cmd.Parameters["@shape"].Value, result);

			Assert.Equal(2 * (double)cmd.Parameters["@radius"].Value, cmd.Parameters["@diameter"].Value);
			Assert.Equal(Math.PI * (double)cmd.Parameters["@radius"].Value, cmd.Parameters["@area"].Value);
			Assert.Equal(Math.PI * Math.Pow((double)cmd.Parameters["@radius"].Value, 2), cmd.Parameters["@area"].Value);
			Assert.Equal((double)cmd.Parameters["@area"].Value * (double)cmd.Parameters["@height"].Value, cmd.Parameters["@volume"].Value);
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
		[InlineData("factor")]
		[InlineData("@factor")]
		[InlineData("?factor")]
		public async Task MultipleRows(string paramaterName)
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = "number_multiples";
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.Parameters.Add(new MySqlParameter { ParameterName = paramaterName, Value = 3 });
				using (var reader = await cmd.ExecuteReaderAsync())
				{
					Assert.True(await reader.ReadAsync());
					Assert.Equal("six", reader.GetString(0));
					Assert.True(await reader.ReadAsync());
					Assert.Equal("three", reader.GetString(0));
					Assert.False(await reader.ReadAsync());
					Assert.False(await reader.NextResultAsync());
				}
			}
		}

		[Fact]
		public async Task MultipleResultSets()
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = "multiple_result_sets";
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.Parameters.Add(new MySqlParameter { ParameterName = "@pivot", Value = 4 });
				using (var reader = await cmd.ExecuteReaderAsync())
				{
					Assert.True(await reader.ReadAsync());
					Assert.Equal("one", reader.GetString(0));
					Assert.True(await reader.ReadAsync());
					Assert.Equal("three", reader.GetString(0));
					Assert.True(await reader.ReadAsync());
					Assert.Equal("two", reader.GetString(0));
					Assert.False(await reader.ReadAsync());
					Assert.True(await reader.NextResultAsync());

					Assert.True(await reader.ReadAsync());
					Assert.Equal("eight", reader.GetString(0));
					Assert.True(await reader.ReadAsync());
					Assert.Equal("five", reader.GetString(0));
					Assert.True(await reader.ReadAsync());
					Assert.Equal("seven", reader.GetString(0));
					Assert.True(await reader.ReadAsync());
					Assert.Equal("six", reader.GetString(0));
					Assert.False(await reader.ReadAsync());
					Assert.False(await reader.NextResultAsync());
				}
			}
		}

		[Fact]
		public async Task InOut()
		{
			var parameter = new MySqlParameter
			{
				ParameterName = "high",
				DbType = DbType.Int32,
				Direction = ParameterDirection.InputOutput,
				Value = 1
			};
			while ((int) parameter.Value < 8)
			{
				using (var cmd = m_database.Connection.CreateCommand())
				{
					var nextValue = (int) parameter.Value + 1;
					cmd.CommandText = "number_lister";
					cmd.CommandType = CommandType.StoredProcedure;
					cmd.Parameters.Add(parameter);
					cmd.Prepare();
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
		}

		[Theory
#if BASELINE
			(Skip = "https://bugs.mysql.com/bug.php?id=84220")
#endif
		]
		[InlineData(false)]
		[InlineData(true)]
		public async Task DottedName(bool useDatabaseName)
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = (useDatabaseName ? $"{m_database.Connection.Database}." : "") + "`dotted.name`";
				cmd.CommandType = CommandType.StoredProcedure;
				using (var reader = await cmd.ExecuteReaderAsync())
				{
					Assert.True(await reader.ReadAsync());
					Assert.Equal(1, reader.GetInt32(0));
					Assert.Equal(2, reader.GetInt32(1));
					Assert.Equal(3, reader.GetInt32(2));
					Assert.False(await reader.ReadAsync());
					Assert.False(await reader.NextResultAsync());
				}
			}
		}

		readonly DatabaseFixture m_database;
	}
}
