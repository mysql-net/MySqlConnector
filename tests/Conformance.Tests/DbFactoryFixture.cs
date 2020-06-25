using System;
using System.Data.Common;
using AdoNet.Specification.Tests;
using MySqlConnector;

namespace Conformance.Tests
{
    public class DbFactoryFixture : IDbFactoryFixture
	{
		public DbFactoryFixture()
		{
			ConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? "Server=localhost;User Id=mysqltest;Password=test;SSL Mode=None";
		}

		public string ConnectionString { get; }
		public DbProviderFactory Factory => MySqlConnectorFactory.Instance;
	}
}
