using System.Threading.Tasks;
using BenchmarkDotNet.Running;
using Benchmarks.Cases;

namespace Benchmarks
{
	internal class Program
	{
		private static async Task Main(string[] args)
		{
			BenchmarkRunner.Run<MySqlBulkImport>();
		}
	}
}
