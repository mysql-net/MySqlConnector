using System;
using System.Data.Common;
using MySql.Data.MySqlClient;
using Xunit;

namespace SideBySide
{
	public class ParameterCollection : IDisposable
	{
		public ParameterCollection()
		{
			m_command = new MySqlCommand();
			m_parameterCollection = m_command.Parameters;
		}

		public void Dispose()
		{
			m_command.Dispose();
		}

		[Theory]
		[InlineData("Baz", 0)]
		[InlineData("@Test", 1)]
		[InlineData("?Foo", 2)]
		[InlineData("Bar", -1)]
		[InlineData("@Bar", -1)]
		[InlineData("?Bar", -1)]
		[InlineData("", -1)]
#if !BASELINE
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
			m_parameterCollection.Add(new MySqlParameter { ParameterName = "Baz", Value = 0 });
			m_parameterCollection.Add(new MySqlParameter { ParameterName = "@Test", Value = 1 });
			m_parameterCollection.Add(new MySqlParameter { ParameterName = "?Foo", Value = 2 });
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
#if !BASELINE
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
#if !BASELINE
		[InlineData("test")] // https://bugs.mysql.com/bug.php?id=93370
		[InlineData("@'test'")]
		[InlineData("@`TEST`")]
#endif
		public void AddDuplicateParameter(string parameterName)
		{
			m_parameterCollection.AddWithValue("@test", 1);
			Assert.Throws<MySqlException>(() => { m_parameterCollection.AddWithValue(parameterName, 2); });
		}

		[Fact]
		public void IndexOfNull()
		{
			Assert.Throws<ArgumentNullException>(() => m_parameterCollection.IndexOf(null));
		}

		[Fact]
		public void Clear()
		{
			m_parameterCollection.Add(new MySqlParameter { ParameterName = "@Test1", Value = 0 });
			m_parameterCollection.Add(new MySqlParameter { ParameterName = "@Test2", Value = 1 });
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
			m_parameterCollection.Add(new MySqlParameter { ParameterName = "@Test1", Value = 0 });
			m_parameterCollection.Add(new MySqlParameter { ParameterName = "@Test2", Value = 1 });
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
			m_parameterCollection.Add(new MySqlParameter { ParameterName = "@Test1", Value = 0 });
			m_parameterCollection.Add(new MySqlParameter { ParameterName = "@Test2", Value = 1 });
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
			m_parameterCollection.Add(new MySqlParameter { ParameterName = "@Test1", Value = 0 });
			m_parameterCollection.Add(new MySqlParameter { ParameterName = "@Test2", Value = 1 });
			Assert.Equal(0, m_parameterCollection.IndexOf("@Test1"));
			Assert.Equal(1, m_parameterCollection.IndexOf("@Test2"));
			m_parameterCollection[0] = new MySqlParameter { ParameterName = "@Test3", Value = 2 };
			Assert.Equal(2, m_parameterCollection.Count);
			Assert.Equal(-1, m_parameterCollection.IndexOf("@Test1"));
			Assert.Equal(0, m_parameterCollection.IndexOf("@Test3"));
			Assert.Equal(1, m_parameterCollection.IndexOf("@Test2"));
		}

		[Fact]
		public void SetParameterString()
		{
			m_parameterCollection.Add(new MySqlParameter { ParameterName = "@Test1", Value = 0 });
			m_parameterCollection.Add(new MySqlParameter { ParameterName = "@Test2", Value = 1 });
			Assert.Equal(0, m_parameterCollection.IndexOf("@Test1"));
			Assert.Equal(1, m_parameterCollection.IndexOf("@Test2"));
			m_parameterCollection["@Test1"] = new MySqlParameter { ParameterName = "@Test3", Value = 2 };
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
			m_parameterCollection.Add(new MySqlParameter { ParameterName = "@Test1", Value = 0 });
			m_parameterCollection.Add(new MySqlParameter { ParameterName = "@Test2", Value = 1 });
			var array = new DbParameter[2];
			m_parameterCollection.CopyTo(array, 0);
			Assert.Same(array[0], m_parameterCollection[0]);
			Assert.Same(array[1], m_parameterCollection[1]);
		}

		[Fact]
		public void CopyToIndex()
		{
			m_parameterCollection.Add(new MySqlParameter { ParameterName = "@Test1", Value = 0 });
			m_parameterCollection.Add(new MySqlParameter { ParameterName = "@Test2", Value = 1 });
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
			m_parameterCollection.Add(new MySqlParameter { ParameterName = "@Test1", Value = 0 });
			m_parameterCollection.Add(new MySqlParameter { ParameterName = "@Test2", Value = 1 });
			Assert.Throws<ArgumentNullException>(() => m_parameterCollection.CopyTo(null, 0));
		}

		[Fact]
		public void CopyToSmallArray()
		{
			m_parameterCollection.Add(new MySqlParameter { ParameterName = "@Test1", Value = 0 });
			m_parameterCollection.Add(new MySqlParameter { ParameterName = "@Test2", Value = 1 });
			Assert.Throws<ArgumentException>(() => m_parameterCollection.CopyTo(new DbParameter[1], 0));
		}

		[Fact]
		public void CopyToIndexOutOfRange()
		{
			m_parameterCollection.Add(new MySqlParameter { ParameterName = "@Test1", Value = 0 });
			m_parameterCollection.Add(new MySqlParameter { ParameterName = "@Test2", Value = 1 });
			Assert.Throws<ArgumentException>(() => m_parameterCollection.CopyTo(new DbParameter[2], 3));
		}

		MySqlCommand m_command;
		MySqlParameterCollection m_parameterCollection;
	}
}
