using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.Configuration;

using MySql.Data.MySqlClient;

namespace SideBySide
{
	public static class AppConfig
	{
		private static IReadOnlyDictionary<string, string> DefaultConfig { get; } =
			new Dictionary<string, string>
			{
				["Data:NoPasswordUser"] = "",
				["Data:SupportsCachedProcedures"] = "false",
				["Data:SupportsJson"] = "false",
			};

		public static string CertsPath => Config.GetValue<string>("Data:CertificatesPath");

		public static string TestDataPath => Config.GetValue<string>("Data:TestData");
		public static string RemoteTestDataPath => Config.GetValue<string>("Data:RemoteTestData");

		private static int _configFirst;

		private static IConfiguration ConfigBuilder { get; } = new ConfigurationBuilder()
			.AddInMemoryCollection(DefaultConfig)
			.AddJsonFile("config.json")
			.Build();

		public static IConfiguration Config
		{
			get
			{
				if (Interlocked.Exchange(ref _configFirst, 1) == 0)
					Console.WriteLine("Config Read");
				return ConfigBuilder;
			}
		}

		public static string ConnectionString => Config.GetValue<string>("Data:ConnectionString");

		public static string PasswordlessUser => Config.GetValue<string>("Data:PasswordlessUser");

		public static string SecondaryDatabase => Config.GetValue<string>("Data:SecondaryDatabase");

		public static ServerFeatures SupportedFeatures => (ServerFeatures) Enum.Parse(typeof(ServerFeatures), Config.GetValue<string>("Data:SupportedFeatures"));

		public static bool SupportsJson => SupportedFeatures.HasFlag(ServerFeatures.Json);

		public static string MySqlBulkLoaderCsvFile => ExpandRemoteVariables(Config.GetValue<string>("Data:MySqlBulkLoaderCsvFile"));
		public static string MySqlBulkLoaderLocalCsvFile => ExpandLocalVariables(Config.GetValue<string>("Data:MySqlBulkLoaderLocalCsvFile"));
		public static string MySqlBulkLoaderTsvFile => ExpandRemoteVariables(Config.GetValue<string>("Data:MySqlBulkLoaderTsvFile"));
		public static string MySqlBulkLoaderLocalTsvFile => ExpandLocalVariables(Config.GetValue<string>("Data:MySqlBulkLoaderLocalTsvFile"));

		public static MySqlConnectionStringBuilder CreateConnectionStringBuilder() => new MySqlConnectionStringBuilder(ConnectionString);

		public static MySqlConnectionStringBuilder CreateSha256ConnectionStringBuilder()
		{
			var csb = CreateConnectionStringBuilder();
			csb.UserID = "sha256user";
			csb.Password = "Sh@256Pa55";
			csb.Database = null;
			return csb;
		}

		// tests can run much slower in CI environments
		public static int TimeoutDelayFactor { get; } = (Environment.GetEnvironmentVariable("APPVEYOR") == "True" || Environment.GetEnvironmentVariable("TRAVIS") == "true") ? 6 : 1;

		private static string ExpandVariables(string value, string replacement) => value?.Replace("%TESTDATA%", replacement);
		private static string ExpandLocalVariables(string value) => ExpandVariables(value, TestDataPath);
		private static string ExpandRemoteVariables(string value) => ExpandVariables(value, RemoteTestDataPath);
	}
}
