using System.Globalization;

namespace SideBySide;

public class DataAdapterTests : IClassFixture<DatabaseFixture>, IDisposable
{
	public DataAdapterTests(DatabaseFixture database)
	{
		m_connection = database.Connection;
		m_connection.Open();

#if BASELINE
		// not sure why this is necessary
		m_connection.Execute("drop table if exists data_adapter;");
#endif

		m_connection.Execute(@"
create temporary table data_adapter(
	id bigint not null primary key auto_increment,
	int_value int null,
	text_value text null
);
insert into data_adapter(int_value, text_value) values
(null, null),
(0, ''),
(1, 'one');
");
	}

	public void Dispose()
	{
		m_connection.Close();
	}

	[Fact]
	public void UseDataAdapter()
	{
		using var command = new MySqlCommand("SELECT 1", m_connection);
		using var da = new MySqlDataAdapter();
		using var ds = new DataSet();
		da.SelectCommand = command;
		da.Fill(ds);
		Assert.Single(ds.Tables);
		Assert.Single(ds.Tables[0].Rows);
		Assert.Single(ds.Tables[0].Rows[0].ItemArray);
		TestUtilities.AssertIsOne(ds.Tables[0].Rows[0][0]);
	}

	[Fact]
	public void UseDataAdapterMySqlConnectionConstructor()
	{
		using var command = new MySqlCommand("SELECT 1", m_connection);
		using var da = new MySqlDataAdapter(command);
		using var ds = new DataSet();
		da.Fill(ds);
		TestUtilities.AssertIsOne(ds.Tables[0].Rows[0][0]);
	}

	[Fact]
	public void UseDataAdapterStringMySqlConnectionConstructor()
	{
		using var da = new MySqlDataAdapter("SELECT 1", m_connection);
		using var ds = new DataSet();
		da.Fill(ds);
		TestUtilities.AssertIsOne(ds.Tables[0].Rows[0][0]);
	}

	[Fact]
	public void UseDataAdapterStringStringConstructor()
	{
		using var da = new MySqlDataAdapter("SELECT 1", AppConfig.ConnectionString);
		using var ds = new DataSet();
		da.Fill(ds);
		TestUtilities.AssertIsOne(ds.Tables[0].Rows[0][0]);
	}

	[Fact]
	public void Fill()
	{
		using var da = new MySqlDataAdapter("select * from data_adapter", m_connection);
		using var ds = new DataSet();
		da.Fill(ds, "data_adapter");

		Assert.Single(ds.Tables);
		Assert.Equal(3, ds.Tables[0].Rows.Count);

		Assert.Equal(1L, ds.Tables[0].Rows[0]["id"]);
		Assert.Equal(2L, ds.Tables[0].Rows[1]["id"]);
		Assert.Equal(3L, ds.Tables[0].Rows[2]["id"]);

		Assert.Equal(DBNull.Value, ds.Tables[0].Rows[0]["int_value"]);
		Assert.Equal(0, ds.Tables[0].Rows[1]["int_value"]);
		Assert.Equal(1, ds.Tables[0].Rows[2]["int_value"]);

		Assert.Equal(DBNull.Value, ds.Tables[0].Rows[0]["text_value"]);
		Assert.Equal("", ds.Tables[0].Rows[1]["text_value"]);
		Assert.Equal("one", ds.Tables[0].Rows[2]["text_value"]);
	}

	[Fact]
	public void LoadDataTable()
	{
		using var command = new MySqlCommand("SELECT * FROM data_adapter", m_connection);
		using var dr = command.ExecuteReader();
		var dt = new DataTable();
		dt.Load(dr);
		dr.Close();

		Assert.Equal(3, dt.Rows.Count);

		Assert.Equal(1L, dt.Rows[0]["id"]);
		Assert.Equal(2L, dt.Rows[1]["id"]);
		Assert.Equal(3L, dt.Rows[2]["id"]);

		Assert.Equal(DBNull.Value, dt.Rows[0]["int_value"]);
		Assert.Equal(0, dt.Rows[1]["int_value"]);
		Assert.Equal(1, dt.Rows[2]["int_value"]);

		Assert.Equal(DBNull.Value, dt.Rows[0]["text_value"]);
		Assert.Equal("", dt.Rows[1]["text_value"]);
		Assert.Equal("one", dt.Rows[2]["text_value"]);
	}

	[SkippableFact(Baseline = "Throws FormatException: Input string was not in a correct format")]
	public void InsertWithDataSet()
	{
		using (var ds = new DataSet())
		using (var da = new MySqlDataAdapter("SELECT * FROM data_adapter", m_connection))
		{
			da.Fill(ds);

			da.InsertCommand = new MySqlCommand("INSERT INTO data_adapter (int_value, text_value) VALUES (@int, @text)", m_connection);

			da.InsertCommand.Parameters.Add(new("@int", DbType.Int32));
			da.InsertCommand.Parameters.Add(new("@text", DbType.String));

			da.InsertCommand.Parameters[0].Direction = ParameterDirection.Input;
			da.InsertCommand.Parameters[1].Direction = ParameterDirection.Input;

			da.InsertCommand.Parameters[0].SourceColumn = "int_value";
			da.InsertCommand.Parameters[1].SourceColumn = "text_value";

			var dt = ds.Tables[0];
			var dr = dt.NewRow();
			dr["int_value"] = 4;
			dr["text_value"] = "four";
			dt.Rows.Add(dr);

			using var ds2 = ds.GetChanges();
			da.Update(ds2);

			ds.Merge(ds2);
			ds.AcceptChanges();
		}

		using var cmd2 = new MySqlCommand("SELECT id, int_value, text_value FROM data_adapter", m_connection);
		using var dr2 = cmd2.ExecuteReader();
		Assert.True(dr2.Read());
		Assert.Equal(1L, dr2[0]);

		Assert.True(dr2.Read());
		Assert.Equal(2L, dr2[0]);

		Assert.True(dr2.Read());
		Assert.Equal(3L, dr2[0]);

		Assert.True(dr2.Read());
		Assert.Equal(4L, dr2[0]);
		Assert.Equal(4, dr2[1]);
		Assert.Equal("four", dr2[2]);
	}

	[Fact]
	public void BatchUpdate()
	{
		using (var ds = new DataSet())
		using (var da = new MySqlDataAdapter("SELECT * FROM data_adapter", m_connection))
		{
			da.Fill(ds);

			da.UpdateCommand = new MySqlCommand("UPDATE data_adapter SET int_value=@int, text_value=@text WHERE id=@id", m_connection)
			{
				Parameters =
				{
					new("@int", MySqlDbType.Int32) { Direction = ParameterDirection.Input, SourceColumn = "int_value" },
					new("@text", MySqlDbType.String) { Direction = ParameterDirection.Input, SourceColumn = "text_value" },
					new("@id", MySqlDbType.Int64) { Direction = ParameterDirection.Input, SourceColumn = "id" },
				},
				UpdatedRowSource = UpdateRowSource.None,
			};

			da.UpdateBatchSize = 10;

			var dt = ds.Tables[0];
			dt.Rows[0][1] = 2;
			dt.Rows[0][2] = "two";
			dt.Rows[1][1] = 3;
			dt.Rows[1][2] = "three";
			dt.Rows[2][1] = 4;
			dt.Rows[2][2] = "four";

			da.Update(ds);
		}

		Assert.Equal(new[] { "two", "three", "four" }, m_connection.Query<string>("SELECT text_value FROM data_adapter ORDER BY id"));
	}


	[Fact]
	public void BatchInsert()
	{
		using (var ds = new DataSet())
		using (var da = new MySqlDataAdapter("SELECT * FROM data_adapter", m_connection))
		{
			da.Fill(ds);

			da.InsertCommand = new MySqlCommand("INSERT INTO data_adapter(int_value, text_value) VALUES(@int, @text);", m_connection)
			{
				Parameters =
				{
					new("@int", MySqlDbType.Int32) { Direction = ParameterDirection.Input, SourceColumn = "int_value" },
					new("@text", MySqlDbType.String) { Direction = ParameterDirection.Input, SourceColumn = "text_value" },
				},
				UpdatedRowSource = UpdateRowSource.None,
			};

			da.UpdateBatchSize = 10;

			var dt = ds.Tables[0];
			dt.Rows.Add(0, 2, "two");
			dt.Rows.Add(0, 3, "three");
			dt.Rows.Add(0, 4, "four");

			da.Update(ds);
		}

		Assert.Equal(new[] { null, "", "one", "two", "three", "four" }, m_connection.Query<string>("SELECT text_value FROM data_adapter ORDER BY id"));
	}

#if !BASELINE
	[Theory]
	[InlineData("INSERT INTO table(col1, col2) VALUES(@col1, @col2);", "@col1,@col2", "0,1")]
	[InlineData("INSERT INTO table(col1, col2) VALUES(@col1, @col2);", "@col2,@col1", "1,0")]
	[InlineData("INSERT INTO table(col1, col2) VALUES(@col1, @col2);", "@col1,@col2,@col3", "0,1")]
	[InlineData("INSERT INTO table(col1, col2) VALUES(@col2, @col3);", "@col1,@col2,@col3", "1,2")]
	[InlineData("INSERT INTO table(col1, col2) VALUES(?, ?);", "@col1,@col2", "0,1")]
	[InlineData("INSERT INTO table(col1, col2)\nVALUES(@col1, @col2);", "@col1,@col2", "0,1")]
	public void ExtractParameterIndexes(string sql, string parameterNames, string expectedIndexes)
	{
		var command = new MySqlCommand(sql, m_connection);
		foreach (var parameterName in parameterNames.Split(','))
			command.Parameters.Add(new MySqlParameter(parameterName, MySqlDbType.Int32));
		var parser = new MySqlDataAdapter.InsertSqlParser(command);
		parser.Parse(sql);
		Assert.Equal(expectedIndexes.Split(',').Select(x => int.Parse(x, CultureInfo.InvariantCulture)), parser.ParameterIndexes);
	}

	[Theory]
	[InlineData("SELECT * FROM table;", 2, 2, null)]
	[InlineData("SELECT * FROM table VALUES;", 2, 2, null)]
	[InlineData("INSERT INTO table VALUES(@param0, @param1)", 2, 2, "INSERT INTO table VALUES(@p0,@p1),(@p2,@p3);")]
	[InlineData("INSERT INTO table VALUES(@param0, @param1)", 3, 2, "INSERT INTO table VALUES(@p0,@p1),(@p2,@p3),(@p4,@p5);")]
	[InlineData("INSERT INTO table VALUES(@param0, @param1)", 2, 3, "INSERT INTO table VALUES(@p0,@p1),(@p2,@p3);")]
	[InlineData("INSERT INTO table VALUES(@param0, @param1, @param2)", 2, 3, "INSERT INTO table VALUES(@p0,@p1,@p2),(@p3,@p4,@p5);")]
	[InlineData("INSERT INTO table VALUES(@param0, @param1);", 2, 2, "INSERT INTO table VALUES(@p0,@p1),(@p2,@p3);")]
	[InlineData("INSERT INTO table VALUES(?, ?)", 2, 2, "INSERT INTO table VALUES(@p0,@p1),(@p2,@p3);")]
	[InlineData("INSERT INTO table  VALUES  (  @param0 , \n @param1  ) ;  ", 2, 2, "INSERT INTO table  VALUES(@p0,@p1),(@p2,@p3);")]
	[InlineData("INSERT INTO table VALUES(@param0, @param2);", 2, 2, null)]
	[InlineData("INSERT INTO table\nVALUES\n(@param0, @param1);", 2, 2, "INSERT INTO table\nVALUES(@p0,@p1),(@p2,@p3);")]
	[InlineData("INSERT INTO `table` VALUES  (@\"param0\", @\"param1\");", 2, 2, "INSERT INTO `table` VALUES(@p0,@p1),(@p2,@p3);")]
	[InlineData("INSERT\nINTO\ntable\nVALUES\n(@param0, @param1);", 2, 2, null)] // ideally should work but \n not supported
	[InlineData("INSERT INTO table VALUES(1, @param0, @param1);", 2, 2, null)]
	public void ConvertBatchToCommand(string insertSql, int commandCount, int parameterCount, string expected)
	{
		var batch = new MySqlBatch(m_connection);
		for (var i = 0; i < commandCount; i++)
		{
			var batchCommand = new MySqlBatchCommand(insertSql);
			for (var j = 0; j < parameterCount; j++)
			{
				batchCommand.Parameters.Add(new MySqlParameter($"@param{j}", MySqlDbType.Int32));
			}
			batch.BatchCommands.Add(batchCommand);
		}

		var command = MySqlDataAdapter.TryConvertToCommand(batch);
		if (expected is null)
		{
			Assert.Null(command);
		}
		else
		{
			Assert.NotNull(command);
			Assert.Equal(expected, command.CommandText);
		}
	}

	[Fact]
	public void ConvertBatchToCommandParameters()
	{
		var insertSql = "INSERT INTO table(col1, col2, col3) VALUES(@c1, @c2, @c1);";
		var batch = new MySqlBatch(m_connection)
		{
			BatchCommands =
			{
				new MySqlBatchCommand(insertSql)
				{
					Parameters =
					{
						new MySqlParameter("@c1", 1),
						new MySqlParameter("@c2", 2),
					},
				},
				new MySqlBatchCommand(insertSql)
				{
					Parameters =
					{
						new MySqlParameter("@c1", 3),
						new MySqlParameter("@c2", 4),
					},
				},
			},
		};

		var command = MySqlDataAdapter.TryConvertToCommand(batch);
		Assert.NotNull(command);
		Assert.Equal("INSERT INTO table(col1, col2, col3) VALUES(@p0,@p1,@p2),(@p3,@p4,@p5);", command.CommandText);
		Assert.Equal(6, command.Parameters.Count);
		Assert.Equal("@p0", command.Parameters[0].ParameterName);
		Assert.Equal(1, command.Parameters[0].Value);
		Assert.Equal("@p1", command.Parameters[1].ParameterName);
		Assert.Equal(2, command.Parameters[1].Value);
		Assert.Equal("@p2", command.Parameters[2].ParameterName);
		Assert.Equal(1, command.Parameters[2].Value);
		Assert.Equal("@p3", command.Parameters[3].ParameterName);
		Assert.Equal(3, command.Parameters[3].Value);
		Assert.Equal("@p4", command.Parameters[4].ParameterName);
		Assert.Equal(4, command.Parameters[4].Value);
		Assert.Equal("@p5", command.Parameters[5].ParameterName);
		Assert.Equal(3, command.Parameters[5].Value);
	}
#endif

	readonly MySqlConnection m_connection;
}
