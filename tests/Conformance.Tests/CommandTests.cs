using AdoNet.Specification.Tests;

namespace Conformance.Tests
{
	public sealed class CommandTests : CommandTestBase<DbFactoryFixture>
	{
		public CommandTests(DbFactoryFixture fixture)
			: base(fixture)
		{
		}
	}
}
