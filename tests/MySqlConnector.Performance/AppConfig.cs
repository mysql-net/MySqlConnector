using System.IO;
using Microsoft.Extensions.Configuration;

namespace MySqlConnector.Performance
{
    public static class AppConfig
    {
        public static IConfigurationRoot Config = new ConfigurationBuilder()
		    .SetBasePath(Directory.GetCurrentDirectory())
		    .AddJsonFile("appsettings.json")
		    .AddJsonFile("config.json")
	    	.Build();
    }
}
