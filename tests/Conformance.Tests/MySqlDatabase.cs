using System;
using AdoNet.Specification.Tests.Databases;

namespace Conformance.Tests
{
    public sealed class MySqlDatabase : MySqlDatabaseBase
	{
		public MySqlDatabase()
		{
			ConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? "Server=localhost;User Id=mysqltest;Password='test;key=\"val';SSL Mode=None";
		}

		public override string ConnectionString { get; }
	}
}
