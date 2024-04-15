namespace MySqlConnector.Tests;

public class MySqlParameterCollectionTests
{
	public MySqlParameterCollectionTests()
	{
		m_collection = new MySqlCommand().Parameters;
	}

	[Fact]
	public void InsertAtNegative() => Assert.Throws<ArgumentOutOfRangeException>(() => m_collection.Insert(-1, new MySqlParameter()));

	[Fact]
	public void InsertPastEnd() => Assert.Throws<ArgumentOutOfRangeException>(() => m_collection.Insert(1, new MySqlParameter()));

	[Fact]
	public void RemoveAtNegative() => Assert.Throws<ArgumentOutOfRangeException>(() => m_collection.RemoveAt(-1));

	[Fact]
	public void RemoveAtEnd() => Assert.Throws<ArgumentOutOfRangeException>(() => m_collection.RemoveAt(0));

	readonly MySqlParameterCollection m_collection;
}
