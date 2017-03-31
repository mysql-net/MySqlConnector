using System;
using System.Linq;
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

		MySqlCommand m_command;
		MySqlParameterCollection m_parameterCollection;
	}
}
