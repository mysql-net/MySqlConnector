using AdoNet.Specification.Tests;
using Xunit;

namespace Conformance.Tests
{
	public sealed class ConnectionTests : ConnectionTestBase<DbFactoryFixture>
	{
		public ConnectionTests(DbFactoryFixture fixture)
			: base(fixture)
		{
		}

		[Fact(Skip = "Throws MySqlException when it attempts to connect.")]
		public override void Set_ConnectionString_throws_when_invalid()
		{
		}

		[Fact(Skip = "Throws MySqlException when it attempts to connect, not InvalidOperationException before connecting")]
		public override void Open_throws_when_no_connection_string()
		{
		}
	}
}
