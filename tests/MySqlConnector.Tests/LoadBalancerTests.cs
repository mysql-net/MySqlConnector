using System.Linq;
using MySqlConnector.Core;
using Xunit;

namespace MySqlConnector.Tests
{
	public class LoadBalancerTests
	{
		[Fact]
		public void FailOver()
		{
			var loadBalancer = FailOverLoadBalancer.Instance;
			var input = new[] { "a", "b", "c", "d" };
			Assert.Equal(new[] { "a", "b", "c", "d" }, loadBalancer.LoadBalance(input));
			Assert.Same(input, loadBalancer.LoadBalance(input));
		}

		[Fact]
		public void RoundRobin()
		{
			var loadBalancer = new RoundRobinLoadBalancer();
			var input = new[] { "a", "b", "c", "d" };
			Assert.Equal(new[] { "a", "b", "c", "d" }, loadBalancer.LoadBalance(input));
			Assert.Equal(new[] { "b", "c", "d", "a" }, loadBalancer.LoadBalance(input));
			Assert.Equal(new[] { "c", "d", "a", "b" }, loadBalancer.LoadBalance(input));
			Assert.Equal(new[] { "d", "a", "b", "c" }, loadBalancer.LoadBalance(input));
			Assert.Equal(new[] { "a", "b", "c", "d" }, loadBalancer.LoadBalance(input));
		}

		[Fact]
		public void Random()
		{
			var loadBalancer = new RoundRobinLoadBalancer();
			var input = new[] { "a", "b", "c", "d" };
			for (int i = 0; i < 10; i++)
			{
				var output = loadBalancer.LoadBalance(input);
				Assert.NotSame(input, output);
				Assert.Equal(input.Length, output.Count());
				Assert.Equal(input, output.OrderBy(x => x));
			}
		}
	}
}
