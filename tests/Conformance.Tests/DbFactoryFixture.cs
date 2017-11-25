using System.Data.Common;
using AdoNet.Specification.Tests.Databases;
using MySql.Data.MySqlClient;

namespace Conformance.Tests
{
    public sealed class DbFactoryFixture : DbFactoryFixtureBase<MySqlDatabase>
	{
		public override DbProviderFactory Factory => MySqlClientFactory.Instance;
	}
}
