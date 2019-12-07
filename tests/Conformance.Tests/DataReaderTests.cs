using AdoNet.Specification.Tests;
using Xunit;

namespace Conformance.Tests
{
	public sealed class DataReaderTests : DataReaderTestBase<SelectValueFixture>
	{
		public DataReaderTests(SelectValueFixture fixture)
			: base(fixture)
		{
		}

		[Fact(Skip = "Deliberately throws InvalidCastException")]
		public override void GetTextReader_returns_empty_for_null_String() { }

		[Fact(Skip = "https://github.com/mysql-net/MySqlConnector/issues/744")]
		public override void GetSchemaTable_throws_after_Delete() { }
	}
}
