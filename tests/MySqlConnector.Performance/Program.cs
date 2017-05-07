using System;
using Microsoft.AspNetCore.Hosting;
using MySqlConnector.Performance.Commands;

namespace MySqlConnector.Performance
{
	public class Program
	{
		public static void Main(string[] args)
		{
			AppDb.Initialize();
			if (args.Length == 0)
			{
				var host = new WebHostBuilder()
					.UseUrls("http://*:5000")
					.UseKestrel()
					.UseStartup<Startup>()
					.Build();
				host.Run();
			}
			else
			{
				Environment.Exit(CommandRunner.Run(args));
			}
		}
	}
}
