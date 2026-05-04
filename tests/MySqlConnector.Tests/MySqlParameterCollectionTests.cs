namespace MySqlConnector.Tests;

public class MySqlParameterCollectionTests : IDisposable
{
	public MySqlParameterCollectionTests()
	{
		m_command = new();
		m_parameterCollection = m_command.Parameters;
	}

	public void Dispose()
	{
		m_command.Dispose();
	}

	[Fact]
	public void AddNullObject()
	{
#if MYSQL_DATA
		Assert.Throws<MySqlException>(() => m_parameterCollection.Add((object) null!));
#else
		Assert.Throws<ArgumentNullException>(() => m_parameterCollection.Add((object) null!));
#endif
	}

	[Fact]
	public void AddNullParameter()
	{
#if MYSQL_DATA
		Assert.Throws<MySqlException>(() => m_parameterCollection.Add((object) null!));
#else
		Assert.Throws<ArgumentNullException>(() => m_parameterCollection.Add((MySqlParameter) null!));
#endif
	}

	[Fact]
	public void AddDuplicateParameterName()
	{
		var parameter1 = new MySqlParameter("@name", 1);
		var parameter2 = new MySqlParameter("name", 2);

		Assert.Same(parameter1, m_parameterCollection.Add(parameter1));
#if MYSQL_DATA
		var exception = Assert.Throws<MySqlException>(() => m_parameterCollection.Add(parameter2));
#else
		var exception = Assert.Throws<ArgumentException>(() => m_parameterCollection.Add(parameter2));
#endif
		Assert.StartsWith("Parameter 'name' has already been defined", exception.Message);

		Assert.Equal(1, m_parameterCollection.Count);
		Assert.Same(parameter1, m_parameterCollection[0]);
	}

	[Fact]
	public void AddSameNamedParameterTwice()
	{
		var parameter = new MySqlParameter("@name", 1);

		Assert.Same(parameter, m_parameterCollection.Add(parameter));
#if MYSQL_DATA
		Assert.Throws<MySqlException>(() => m_parameterCollection.Add(parameter));
#else
		var exception = Assert.Throws<ArgumentException>(() => m_parameterCollection.Add(parameter));
		Assert.StartsWith("The parameter is already contained by this MySqlParameterCollection", exception.Message);
#endif

		Assert.Equal(1, m_parameterCollection.Count);
		Assert.Same(parameter, m_parameterCollection[0]);
	}

	[Fact]
	public void AddSameUnnamedParameterTwice()
	{
		var parameter = new MySqlParameter();

		Assert.Same(parameter, m_parameterCollection.Add(parameter));
#if MYSQL_DATA
		Assert.Throws<MySqlException>(() => m_parameterCollection.Add(parameter));
#else
		var exception = Assert.Throws<ArgumentException>(() => m_parameterCollection.Add(parameter));
		Assert.StartsWith("The parameter is already contained by this MySqlParameterCollection", exception.Message);
#endif

		Assert.Equal(1, m_parameterCollection.Count);
		Assert.Same(parameter, m_parameterCollection[0]);
	}

	[Fact]
	public void AddDistinctUnnamedParameters()
	{
		var parameter1 = new MySqlParameter();
		var parameter2 = new MySqlParameter();

		Assert.Same(parameter1, m_parameterCollection.Add(parameter1));
		Assert.Same(parameter2, m_parameterCollection.Add(parameter2));

		Assert.Equal(2, m_parameterCollection.Count);
		Assert.Same(parameter1, m_parameterCollection[0]);
		Assert.Same(parameter2, m_parameterCollection[1]);
	}

	[Fact]
	public void AddParameterFromAnotherCollection()
	{
		var parameter = new MySqlParameter("@name", 1);
		var otherCollection = new MySqlCommand().Parameters;
		otherCollection.Add(parameter);

#if !MYSQL_DATA
		var exception = Assert.Throws<ArgumentException>(() => m_parameterCollection.Add(parameter));
		Assert.StartsWith("The parameter is already contained by another MySqlParameterCollection", exception.Message);
#endif

		Assert.Equal(0, m_parameterCollection.Count);
		Assert.Equal(1, otherCollection.Count);
		Assert.Same(parameter, otherCollection[0]);
	}

	[Fact]
	public void SetParameterByIndexDuplicateParameterLeavesCollectionUnchanged()
	{
		var parameter1 = m_parameterCollection.AddWithValue("@one", 1);
		var parameter2 = m_parameterCollection.AddWithValue("@two", 2);

		Action action = () => m_parameterCollection[0] = parameter2;
		var exception = Assert.Throws<ArgumentException>(action);
#if !MYSQL_DATA
		Assert.StartsWith("The parameter is already contained by this MySqlParameterCollection", exception.Message);
#endif

		Assert.Equal(2, m_parameterCollection.Count);
#if !MYSQL_DATA
		Assert.Same(parameter1, m_parameterCollection[0]);
		Assert.Equal(0, m_parameterCollection.IndexOf("@one"));
#endif
		Assert.Same(parameter2, m_parameterCollection[1]);
		Assert.Equal(1, m_parameterCollection.IndexOf("@two"));
	}

	[Fact]
	public void SetParameterByIndexDuplicateNameLeavesCollectionUnchanged()
	{
		var parameter1 = m_parameterCollection.AddWithValue("@one", 1);
		var parameter2 = m_parameterCollection.AddWithValue("@two", 2);
		var replacement = new MySqlParameter("@two", 3);

		Action action = () => m_parameterCollection[0] = replacement;
		var exception = Assert.Throws<ArgumentException>(action);
#if !MYSQL_DATA
		Assert.StartsWith("Parameter '@two' has already been defined", exception.Message);
#endif

		Assert.Equal(2, m_parameterCollection.Count);
#if !MYSQL_DATA
		Assert.Same(parameter1, m_parameterCollection[0]);
		Assert.Equal(0, m_parameterCollection.IndexOf("@one"));
#endif
		Assert.Same(parameter2, m_parameterCollection[1]);
		Assert.Equal(1, m_parameterCollection.IndexOf("@two"));
	}

	[Fact]
	public void SetParameterByIndexSameParameterIsNoOp()
	{
		var parameter = m_parameterCollection.AddWithValue("@one", 1);

		m_parameterCollection[0] = parameter;

		Assert.Equal(1, m_parameterCollection.Count);
		Assert.Same(parameter, m_parameterCollection[0]);
		Assert.Equal(0, m_parameterCollection.IndexOf("@one"));
	}

	[Fact]
	public void SetParameterByNameDuplicateNameLeavesCollectionUnchanged()
	{
		var parameter1 = m_parameterCollection.AddWithValue("@one", 1);
		var parameter2 = m_parameterCollection.AddWithValue("@two", 2);
		var replacement = new MySqlParameter("@two", 3);

		Action action = () => m_parameterCollection["@one"] = replacement;
		var exception = Assert.Throws<ArgumentException>(action);
#if !MYSQL_DATA
		Assert.StartsWith("Parameter '@two' has already been defined", exception.Message);
#endif

		Assert.Equal(2, m_parameterCollection.Count);
#if !MYSQL_DATA
		Assert.Same(parameter1, m_parameterCollection[0]);
		Assert.Equal(0, m_parameterCollection.IndexOf("@one"));
#endif
		Assert.Same(parameter2, m_parameterCollection[1]);
		Assert.Equal(1, m_parameterCollection.IndexOf("@two"));
	}

	[Fact]
	public void InsertAtNegative() => Assert.Throws<ArgumentOutOfRangeException>(() => m_parameterCollection.Insert(-1, new MySqlParameter()));

	[Fact]
	public void InsertPastEnd() => Assert.Throws<ArgumentOutOfRangeException>(() => m_parameterCollection.Insert(1, new MySqlParameter()));

	[Fact]
	public void RemoveAtNegative() => Assert.Throws<ArgumentOutOfRangeException>(() => m_parameterCollection.RemoveAt(-1));

	[Fact]
	public void RemoveAtEnd() => Assert.Throws<ArgumentOutOfRangeException>(() => m_parameterCollection.RemoveAt(0));

	[Theory]
	[InlineData("Baz", 0)]
	[InlineData("@Test", 1)]
	[InlineData("?Foo", 2)]
	[InlineData("Bar", -1)]
	[InlineData("@Bar", -1)]
	[InlineData("?Bar", -1)]
	[InlineData("", -1)]
#if !MYSQL_DATA
	[InlineData("@Baz", 0)]
	[InlineData("?Baz", 0)]
	[InlineData("@'Baz'", 0)]
	[InlineData("?Test", 1)]
	[InlineData("Test", 1)]
	[InlineData("@`Test`", 1)]
	[InlineData("Foo", 2)]
	[InlineData("@Foo", 2)]
	[InlineData("@\"Foo\"", 2)]
#endif
	public void FindByName(string parameterName, int position)
	{
		m_parameterCollection.Add(new() { ParameterName = "Baz", Value = 0 });
		m_parameterCollection.Add(new() { ParameterName = "@Test", Value = 1 });
		m_parameterCollection.Add(new() { ParameterName = "?Foo", Value = 2 });
		int index = m_parameterCollection.IndexOf(parameterName);
		Assert.Equal(position, index);
		Assert.Equal(position != -1, m_parameterCollection.Contains(parameterName));

		if (position == -1)
		{
			Assert.Throws<ArgumentException>(() => m_parameterCollection[parameterName]);
		}
		else
		{
			var parameter = m_parameterCollection[parameterName];
			Assert.NotNull(parameter);
			Assert.Same(m_parameterCollection[position], parameter);
		}
	}

	[Theory]
	[InlineData("@test")]
	[InlineData("@Test")]
	[InlineData("@tEsT")]
	[InlineData("@TEST")]
#if !MYSQL_DATA
	[InlineData("test")]
	[InlineData("@`test`")]
	[InlineData("@'test'")]
#endif
	public void FindByNameIgnoringCase(string parameterName)
	{
		m_parameterCollection.AddWithValue("@Test", 1);
		Assert.Equal(1, m_parameterCollection.Count);

		Assert.True(m_parameterCollection.Contains(parameterName));
		Assert.Equal(0, m_parameterCollection.IndexOf(parameterName));

		var parameter = m_parameterCollection[parameterName];
		Assert.Equal("@Test", parameter.ParameterName);
		Assert.Equal(1, parameter.Value);

		parameter = m_parameterCollection[0];
		Assert.Equal("@Test", parameter.ParameterName);

		m_parameterCollection.RemoveAt(parameterName);
		Assert.Equal(0, m_parameterCollection.Count);
	}

	[Theory]
	[InlineData("@test")]
	[InlineData("@TEST")]
	[InlineData("test")]
#if !MYSQL_DATA // doesn't support quoted parameter names
	[InlineData("@'test'")]
	[InlineData("@`TEST`")]
#endif
	public void AddDuplicateParameter(string parameterName)
	{
		m_parameterCollection.AddWithValue("@test", 1);
#if MYSQL_DATA
		Assert.Throws<MySqlException>(() => { m_parameterCollection.AddWithValue(parameterName, 2); });
#else
		Assert.Throws<ArgumentException>(() => { m_parameterCollection.AddWithValue(parameterName, 2); });
#endif
	}

	[Fact]
	public void IndexOfNull()
	{
#if !MYSQL_DATA
		Assert.Equal(-1, m_parameterCollection.IndexOf(null));
#else
		Assert.Throws<ArgumentNullException>(() => m_parameterCollection.IndexOf(null));
#endif
	}

	[Fact]
	public void Clear()
	{
		m_parameterCollection.Add(new() { ParameterName = "@Test1", Value = 0 });
		m_parameterCollection.Add(new() { ParameterName = "@Test2", Value = 1 });
		Assert.Equal(0, m_parameterCollection.IndexOf("@Test1"));
		Assert.Equal(1, m_parameterCollection.IndexOf("@Test2"));
		m_parameterCollection.Clear();
		Assert.Equal(0, m_parameterCollection.Count);
		Assert.Equal(-1, m_parameterCollection.IndexOf("@Test1"));
		Assert.Equal(-1, m_parameterCollection.IndexOf("@Test2"));
	}

	[Fact]
	public void RemoveAtIndex()
	{
		m_parameterCollection.Add(new() { ParameterName = "@Test1", Value = 0 });
		m_parameterCollection.Add(new() { ParameterName = "@Test2", Value = 1 });
		Assert.Equal(0, m_parameterCollection.IndexOf("@Test1"));
		Assert.Equal(1, m_parameterCollection.IndexOf("@Test2"));
		m_parameterCollection.RemoveAt(0);
		Assert.Equal(1, m_parameterCollection.Count);
		Assert.Equal(-1, m_parameterCollection.IndexOf("@Test1"));
		Assert.Equal(0, m_parameterCollection.IndexOf("@Test2"));
	}

	[Fact]
	public void RemoveAtString()
	{
		m_parameterCollection.Add(new() { ParameterName = "@Test1", Value = 0 });
		m_parameterCollection.Add(new() { ParameterName = "@Test2", Value = 1 });
		Assert.Equal(0, m_parameterCollection.IndexOf("@Test1"));
		Assert.Equal(1, m_parameterCollection.IndexOf("@Test2"));
		m_parameterCollection.RemoveAt("@Test1");
		Assert.Equal(1, m_parameterCollection.Count);
		Assert.Equal(-1, m_parameterCollection.IndexOf("@Test1"));
		Assert.Equal(0, m_parameterCollection.IndexOf("@Test2"));
	}

	[Fact]
	public void SetParameterIndex()
	{
		m_parameterCollection.Add(new() { ParameterName = "@Test1", Value = 0 });
		m_parameterCollection.Add(new() { ParameterName = "@Test2", Value = 1 });
		Assert.Equal(0, m_parameterCollection.IndexOf("@Test1"));
		Assert.Equal(1, m_parameterCollection.IndexOf("@Test2"));
		m_parameterCollection[0] = new() { ParameterName = "@Test3", Value = 2 };
		Assert.Equal(2, m_parameterCollection.Count);
		Assert.Equal(-1, m_parameterCollection.IndexOf("@Test1"));
		Assert.Equal(0, m_parameterCollection.IndexOf("@Test3"));
		Assert.Equal(1, m_parameterCollection.IndexOf("@Test2"));
	}

	[Fact]
	public void SetParameterString()
	{
		m_parameterCollection.Add(new() { ParameterName = "@Test1", Value = 0 });
		m_parameterCollection.Add(new() { ParameterName = "@Test2", Value = 1 });
		Assert.Equal(0, m_parameterCollection.IndexOf("@Test1"));
		Assert.Equal(1, m_parameterCollection.IndexOf("@Test2"));
		m_parameterCollection["@Test1"] = new() { ParameterName = "@Test3", Value = 2 };
		Assert.Equal(2, m_parameterCollection.Count);
		Assert.Equal(-1, m_parameterCollection.IndexOf("@Test1"));
		Assert.Equal(0, m_parameterCollection.IndexOf("@Test3"));
		Assert.Equal(1, m_parameterCollection.IndexOf("@Test2"));
		m_parameterCollection.AddWithValue("@Test4", 2);
		Assert.Equal(2, m_parameterCollection.IndexOf("@Test4"));
		Assert.Equal(3, m_parameterCollection.Count);
	}

	[Fact]
	public void AddMySqlParameter()
	{
		var parameter = new MySqlParameter("test", MySqlDbType.Double, 3);
		var parameter2 = m_parameterCollection.Add(parameter);
		Assert.Same(parameter, parameter2);
		Assert.Same(parameter, m_parameterCollection[0]);
	}

	[Fact]
	public void AddNameType()
	{
		var parameter = m_parameterCollection.Add("test", MySqlDbType.Double);
		Assert.Equal("test", parameter.ParameterName);
		Assert.Equal(MySqlDbType.Double, parameter.MySqlDbType);
		Assert.Same(parameter, m_parameterCollection[0]);
	}

	[Fact]
	public void AddNameTypeSize()
	{
		var parameter = m_parameterCollection.Add("test", MySqlDbType.Double, 10);
		Assert.Equal("test", parameter.ParameterName);
		Assert.Equal(MySqlDbType.Double, parameter.MySqlDbType);
		Assert.Equal(10, parameter.Size);
		Assert.Same(parameter, m_parameterCollection[0]);
	}

	[Fact]
	public void CopyTo()
	{
		m_parameterCollection.Add(new() { ParameterName = "@Test1", Value = 0 });
		m_parameterCollection.Add(new() { ParameterName = "@Test2", Value = 1 });
		var array = new DbParameter[2];
		m_parameterCollection.CopyTo(array, 0);
		Assert.Same(array[0], m_parameterCollection[0]);
		Assert.Same(array[1], m_parameterCollection[1]);
	}

	[Fact]
	public void CopyToIndex()
	{
		m_parameterCollection.Add(new() { ParameterName = "@Test1", Value = 0 });
		m_parameterCollection.Add(new() { ParameterName = "@Test2", Value = 1 });
		var array = new DbParameter[4];
		m_parameterCollection.CopyTo(array, 1);
		Assert.Null(array[0]);
		Assert.Same(array[1], m_parameterCollection[0]);
		Assert.Same(array[2], m_parameterCollection[1]);
		Assert.Null(array[3]);
	}

	[Fact]
	public void CopyToNullArray()
	{
		m_parameterCollection.Add(new() { ParameterName = "@Test1", Value = 0 });
		m_parameterCollection.Add(new() { ParameterName = "@Test2", Value = 1 });
		Assert.Throws<ArgumentNullException>(() => m_parameterCollection.CopyTo(null, 0));
	}

	[Fact]
	public void CopyToSmallArray()
	{
		m_parameterCollection.Add(new() { ParameterName = "@Test1", Value = 0 });
		m_parameterCollection.Add(new() { ParameterName = "@Test2", Value = 1 });
		Assert.Throws<ArgumentException>(() => m_parameterCollection.CopyTo(new DbParameter[1], 0));
	}

	[Fact]
	public void CopyToIndexOutOfRange()
	{
		m_parameterCollection.Add(new() { ParameterName = "@Test1", Value = 0 });
		m_parameterCollection.Add(new() { ParameterName = "@Test2", Value = 1 });
		Assert.Throws<ArgumentException>(() => m_parameterCollection.CopyTo(new DbParameter[2], 3));
	}

	[Fact]
	public void ChangeParameterNameAfterAdd()
	{
		var parameter = m_command.CreateParameter();
		parameter.ParameterName = "@a";

		m_parameterCollection.Add(parameter);

		parameter.ParameterName = "@b";

		Assert.Equal(parameter, m_parameterCollection["@b"]);
		Assert.Throws<ArgumentException>(() => m_parameterCollection["@a"]);
	}

	[Fact]
	public void SetParameterNameAfterAdd()
	{
		var parameter = m_command.CreateParameter();
		m_parameterCollection.Add(parameter);
		parameter.ParameterName = "@a";
		Assert.Equal(parameter, m_parameterCollection["@a"]);
	}

	[Fact]
	public void SetTwoParametersToSameName()
	{
		var parameter1 = m_command.CreateParameter();
		m_parameterCollection.Add(parameter1);
		var parameter2 = m_command.CreateParameter();
		m_parameterCollection.Add(parameter2);
		parameter1.ParameterName = "@a";
		Assert.Throws<ArgumentException>(() => parameter2.ParameterName = "@a");
	}

	private readonly MySqlCommand m_command;
	private readonly MySqlParameterCollection m_parameterCollection;
}
