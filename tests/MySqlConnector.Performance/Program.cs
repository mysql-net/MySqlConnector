using Microsoft.AspNetCore.Hosting;

namespace MySqlConnector.Performance
{
	public class Program
	{
		public static void Main(string[] args)
		{
			AppDb.Initialize();
			var host = new WebHostBuilder()
				.UseKestrel()
				.UseStartup<Startup>()
				.Build();

			host.Run();
		}
	}
}
