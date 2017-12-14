using AdoNet.Specification.Tests;

namespace Conformance.Tests
{
	public sealed class DataReaderTests : DataReaderTestBase<SelectValueFixture>
	{
		public DataReaderTests(SelectValueFixture fixture)
			: base(fixture)
		{
		}
	}
}
