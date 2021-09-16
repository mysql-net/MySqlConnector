using AdoNet.Specification.Tests;

namespace Conformance.Tests;

public sealed class TransactionTests : TransactionTestBase<DbFactoryFixture>
{
	public TransactionTests(DbFactoryFixture fixture)
		: base(fixture)
	{
	}
}
