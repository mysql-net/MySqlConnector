#if MYSQL_DATA
using MySql.Data.MySqlClient;
#endif
using Xunit;

namespace MySqlConnector.Tests;

public class MySqlAttributeCollectionTests
{
	public MySqlAttributeCollectionTests()
	{
		m_collection = new MySqlCommand().Attributes;
		m_attribute = new MySqlAttribute("name", "value");
	}

	[Fact]
	public void EmptyCount()
	{
		AssertEmpty();
	}

	[Fact]
	public void CountAfterAdd()
	{
		AddAttribute();
		AssertSingle();
	}

	[Fact]
	public void ClearAfterAdd()
	{
		AddAttribute();
		m_collection.Clear();
		AssertEmpty();
	}

	[Fact]
	public void GetAtIndex()
	{
		AddAttribute();
		Assert.Same(m_attribute, m_collection[0]);
	}

	[Fact]
	public void SetAttribute()
	{
		m_collection.SetAttribute("name2", "value2");
		var attribute = AssertSingle();
		Assert.Equal("name2", attribute.AttributeName);
		Assert.Equal("value2", attribute.Value);
	}

#if !MYSQL_DATA
	[Fact]
	public void SetAttributeTwice()
	{
		m_collection.SetAttribute("name2", "value2");
		m_collection.SetAttribute("name2", "value3");
		var attribute = AssertSingle();
		Assert.Equal("name2", attribute.AttributeName);
		Assert.Equal("value3", attribute.Value);
	}
#endif

	private void AddAttribute()
	{
#if MYSQL_DATA
		m_collection.SetAttribute(m_attribute);
#else
		m_collection.Add(m_attribute);
#endif
	}

	private void AssertEmpty()
	{
#if MYSQL_DATA
		Assert.Equal(0, m_collection.Count);
#else
		Assert.Empty(m_collection);
#endif
	}

	private MySqlAttribute AssertSingle()
	{
#if MYSQL_DATA
		Assert.Equal(1, m_collection.Count);
		return m_collection[0];
#else
		return (MySqlAttribute) Assert.Single(m_collection);
#endif
	}

	private readonly MySqlAttributeCollection m_collection;
	private readonly MySqlAttribute m_attribute;
}
