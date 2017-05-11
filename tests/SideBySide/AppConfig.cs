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

		private static string CodeRootPath = GetCodeRootPath();

		public static string BasePath = Path.Combine(CodeRootPath, "tests", "SideBySide");

		public static string CertsPath = Path.Combine(CodeRootPath, ".ci", "server", "certs");

		public static string TestDataPath = Path.Combine(CodeRootPath, "tests", "TestData");

		private static int _configFirst;

		private static IConfiguration ConfigBuilder { get; } = new ConfigurationBuilder()
			.SetBasePath(BasePath)
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

		public static bool SupportsCachedProcedures => Config.GetValue<bool>("Data:SupportsCachedProcedures");

		public static bool SupportsJson => Config.GetValue<bool>("Data:SupportsJson");

		public static string MySqlBulkLoaderCsvFile => ExpandVariables(Config.GetValue<string>("Data:MySqlBulkLoaderCsvFile"));
		public static string MySqlBulkLoaderLocalCsvFile => ExpandVariables(Config.GetValue<string>("Data:MySqlBulkLoaderLocalCsvFile"));
		public static string MySqlBulkLoaderTsvFile => ExpandVariables(Config.GetValue<string>("Data:MySqlBulkLoaderTsvFile"));
		public static string MySqlBulkLoaderLocalTsvFile => ExpandVariables(Config.GetValue<string>("Data:MySqlBulkLoaderLocalTsvFile"));

		public static MySqlConnectionStringBuilder CreateConnectionStringBuilder()
		{
			return new MySqlConnectionStringBuilder(ConnectionString);
		}

		// tests can run much slower in CI environments
		public static int TimeoutDelayFactor { get; } = (Environment.GetEnvironmentVariable("APPVEYOR") == "True" || Environment.GetEnvironmentVariable("TRAVIS") == "true") ? 6 : 1;

		private static string GetCodeRootPath()
		{
#if NET46
			var currentAssembly = Assembly.GetExecutingAssembly();
#else
			var currentAssembly = typeof(AppConfig).GetTypeInfo().Assembly;
#endif
			var directory = new Uri(currentAssembly.CodeBase).LocalPath;
			while (!string.Equals(Path.GetFileName(directory), "MySqlConnector", StringComparison.OrdinalIgnoreCase))
				directory = Path.GetDirectoryName(directory);
			return directory;
		}

		private static string ExpandVariables(string value) => value?.Replace("%TESTDATA%", TestDataPath);
	}
}
