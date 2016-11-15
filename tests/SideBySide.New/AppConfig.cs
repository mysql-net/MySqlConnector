using System;
using System.Collections.Generic;
using System.IO;
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

		private static int _configFirst;

		private static IConfiguration ConfigBuilder { get; } = new ConfigurationBuilder()
			.SetBasePath(File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "config.json")) || new DirectoryInfo(Directory.GetCurrentDirectory()).Name == "SideBySide.New"
				? Directory.GetCurrentDirectory() : Path.Combine(Directory.GetCurrentDirectory(), "tests", "SideBySide.New"))
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
	}
}
