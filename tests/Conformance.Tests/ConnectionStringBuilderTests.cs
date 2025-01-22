using AdoNet.Specification.Tests;

namespace Conformance.Tests;

public sealed class ConnectionStringBuilderTests : ConnectionStringTestBase<DbFactoryFixture>
{
	public ConnectionStringBuilderTests(DbFactoryFixture fixture)
		: base(fixture)
	{
	}
}
