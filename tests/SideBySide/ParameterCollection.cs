using System;
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
		[InlineData("@Baz", -1)]
		[InlineData("?Baz", -1)]
		[InlineData("Test", -1)]
		[InlineData("@Test", 1)]
		[InlineData("?Test", -1)]
		[InlineData("Foo", -1)]
		[InlineData("@Foo", -1)]
		[InlineData("?Foo", 2)]
		[InlineData("Bar", -1)]
		[InlineData("@Bar", -1)]
		[InlineData("?Bar", -1)]
		[InlineData("", -1)]
		public void FindByName(string parameterName, int position)
		{
			m_parameterCollection.Add(new MySqlParameter { ParameterName = "Baz", Value = 0 });
			m_parameterCollection.Add(new MySqlParameter { ParameterName = "@Test", Value = 1 });
			m_parameterCollection.Add(new MySqlParameter { ParameterName = "?Foo", Value = 2 });
			int index = m_parameterCollection.IndexOf(parameterName);
			Assert.Equal(position, index);
			Assert.Equal(position != -1, m_parameterCollection.Contains(parameterName));

			string expectedParameterName = parameterName;
#if BASELINE
			// NOTE: Baseline will match "Baz" in the parameter collection with "@Baz", but only for the indexing operator
			if (parameterName.EndsWith("Baz", StringComparison.Ordinal))
			{
				position = 0;
				expectedParameterName = "Baz";
			}
#endif
			if (position == -1)
			{
				Assert.Throws<ArgumentException>(() => m_parameterCollection[parameterName]);
			}
			else
			{
				var parameter = m_parameterCollection[parameterName];
				Assert.NotNull(parameter);
				Assert.Equal(expectedParameterName, parameter.ParameterName);
			}
		}

		[Theory]
		[InlineData("@test")]
		[InlineData("@Test")]
		[InlineData("@tEsT")]
		[InlineData("@TEST")]
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

		MySqlCommand m_command;
		MySqlParameterCollection m_parameterCollection;
	}
}
