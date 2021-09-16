using Xunit;

namespace MySqlConnector.Tests;

public class MySqlParameterCollectionNameToIndexTests
{
	public MySqlParameterCollectionNameToIndexTests()
	{
		m_collection = new MySqlParameterCollection
		{
			new MySqlParameter { ParameterName = "A", Value = 1 },
			new MySqlParameter { ParameterName = "B", Value = 2 },
			new MySqlParameter { ParameterName = "C", Value = 3 },
		};
	}

	[Fact]
	public void IgnoreCase()
	{
		Assert.Equal(0, m_collection.NormalizedIndexOf("a"));
		Assert.Equal(1, m_collection.NormalizedIndexOf("b"));
		Assert.Equal(2, m_collection.NormalizedIndexOf("c"));
	}

	[Fact]
	public void RemoveFirst()
	{
		m_collection.Remove(m_collection[0]);
		Assert.Equal(-1, m_collection.NormalizedIndexOf("A"));
		Assert.Equal(0, m_collection.NormalizedIndexOf("B"));
		Assert.Equal(1, m_collection.NormalizedIndexOf("C"));
	}

	[Fact]
	public void RemoveSecond()
	{
		m_collection.Remove(m_collection[1]);
		Assert.Equal(0, m_collection.NormalizedIndexOf("A"));
		Assert.Equal(-1, m_collection.NormalizedIndexOf("B"));
		Assert.Equal(1, m_collection.NormalizedIndexOf("C"));
	}

	[Fact]
	public void RemoveLast()
	{
		m_collection.Remove(m_collection[2]);
		Assert.Equal(0, m_collection.NormalizedIndexOf("A"));
		Assert.Equal(1, m_collection.NormalizedIndexOf("B"));
		Assert.Equal(-1, m_collection.NormalizedIndexOf("C"));
	}

	[Fact]
	public void RemoveAtBeginning()
	{
		m_collection.RemoveAt(0);
		Assert.Equal(-1, m_collection.NormalizedIndexOf("A"));
		Assert.Equal(0, m_collection.NormalizedIndexOf("B"));
		Assert.Equal(1, m_collection.NormalizedIndexOf("C"));
	}

	[Fact]
	public void RemoveAtMiddle()
	{
		m_collection.RemoveAt(1);
		Assert.Equal(0, m_collection.NormalizedIndexOf("A"));
		Assert.Equal(-1, m_collection.NormalizedIndexOf("B"));
		Assert.Equal(1, m_collection.NormalizedIndexOf("C"));
	}

	[Fact]
	public void RemoveAtEnd()
	{
		m_collection.RemoveAt(2);
		Assert.Equal(0, m_collection.NormalizedIndexOf("A"));
		Assert.Equal(1, m_collection.NormalizedIndexOf("B"));
		Assert.Equal(-1, m_collection.NormalizedIndexOf("C"));
	}

	[Fact]
	public void InsertBeginning()
	{
		m_collection.Insert(0, new MySqlParameter { ParameterName = "D", Value = 4 });
		Assert.Equal(1, m_collection.NormalizedIndexOf("A"));
		Assert.Equal(2, m_collection.NormalizedIndexOf("B"));
		Assert.Equal(3, m_collection.NormalizedIndexOf("C"));
		Assert.Equal(0, m_collection.NormalizedIndexOf("D"));
	}

	[Fact]
	public void InsertMiddle()
	{
		m_collection.Insert(1, new MySqlParameter { ParameterName = "D", Value = 4 });
		Assert.Equal(0, m_collection.NormalizedIndexOf("A"));
		Assert.Equal(2, m_collection.NormalizedIndexOf("B"));
		Assert.Equal(3, m_collection.NormalizedIndexOf("C"));
		Assert.Equal(1, m_collection.NormalizedIndexOf("D"));
	}

	[Fact]
	public void InsertEnd()
	{
		m_collection.Insert(3, new MySqlParameter { ParameterName = "D", Value = 4 });
		Assert.Equal(0, m_collection.NormalizedIndexOf("A"));
		Assert.Equal(1, m_collection.NormalizedIndexOf("B"));
		Assert.Equal(2, m_collection.NormalizedIndexOf("C"));
		Assert.Equal(3, m_collection.NormalizedIndexOf("D"));
	}

	[Fact]
	public void AddEnd()
	{
		m_collection.Add(new MySqlParameter { ParameterName = "D", Value = 4 });
		Assert.Equal(0, m_collection.NormalizedIndexOf("A"));
		Assert.Equal(1, m_collection.NormalizedIndexOf("B"));
		Assert.Equal(2, m_collection.NormalizedIndexOf("C"));
		Assert.Equal(3, m_collection.NormalizedIndexOf("D"));
	}

	[Fact]
	public void ChangeParameter()
	{
		m_collection[1] = new MySqlParameter { ParameterName = "D", Value = 4 };
		Assert.Equal(0, m_collection.NormalizedIndexOf("A"));
		Assert.Equal(-1, m_collection.NormalizedIndexOf("B"));
		Assert.Equal(2, m_collection.NormalizedIndexOf("C"));
		Assert.Equal(1, m_collection.NormalizedIndexOf("D"));
	}

	MySqlParameterCollection m_collection;
}
