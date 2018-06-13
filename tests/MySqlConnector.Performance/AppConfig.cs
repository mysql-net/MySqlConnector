using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace MySqlConnector.Performance
{
	public static class AppConfig
	{
		public static IConfigurationRoot Config => LazyConfig.Value;

		private static readonly Lazy<IConfigurationRoot> LazyConfig = new Lazy<IConfigurationRoot>(() => new ConfigurationBuilder()
			.SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
			.AddJsonFile("appsettings.json")
			.AddJsonFile("config.json")
			.Build());
	}
}
