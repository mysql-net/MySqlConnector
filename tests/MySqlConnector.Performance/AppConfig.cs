using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace MySqlConnector.Performance
{
    public static class AppConfig
    {
        public static readonly string MySqlDriver = Environment.GetEnvironmentVariable("MYSQL_DRIVER")?.ToLower();

        private static IConfigurationRoot _config;
        public static IConfigurationRoot Config
        {
            get
            {
                if (_config == null)
                {
                    var builder = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json")
                        .AddJsonFile("config.json");
                    _config = builder.Build();
                }
                return _config;
            }
        }
    }
}
