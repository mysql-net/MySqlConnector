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

		public static string BasePath = Path.Combine(CodeRootPath, "tests", "SideBySide.New");

		public static string CertsPath = Path.Combine(CodeRootPath, ".ci", "server", "certs");

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

		public static bool SupportsCachedProcedures => Config.GetValue<bool>("Data:SupportsCachedProcedures");

		public static bool SupportsJson => Config.GetValue<bool>("Data:SupportsJson");

		public static MySqlConnectionStringBuilder CreateConnectionStringBuilder()
		{
			return new MySqlConnectionStringBuilder(ConnectionString);
		}

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
	}
}
