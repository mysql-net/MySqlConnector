using AdoNet.Specification.Tests;

namespace Conformance.Tests
{
	public sealed class DataReaderTests : DataReaderTestBase<DbFactoryFixture>
	{
		public DataReaderTests(DbFactoryFixture fixture)
			: base(fixture)
		{
		}
	}
}
