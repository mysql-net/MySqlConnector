using AdoNet.Specification.Tests;
using Xunit;

namespace Conformance.Tests
{
	public sealed class ParameterTests : ParameterTestBase<DbFactoryFixture>
	{
		public ParameterTests(DbFactoryFixture fixture)
			: base(fixture)
		{
		}

		[Fact(Skip = "Allows `null` as well as `DBNull.value` for backwards compatibility.")]
		public override void Bind_requires_set_value()
		{
		}
	}
}
