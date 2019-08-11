using System;
using System.Collections.Generic;

namespace MySqlConnector.Core
{
	internal interface ILoadBalancer
	{
		/// <summary>
		/// Returns an <see cref="IEnumerable{String}"/> containing <paramref name="hosts"/> in the order they
		/// should be tried to satisfy the load balancing policy.
		/// </summary>
		IEnumerable<string> LoadBalance(IReadOnlyList<string> hosts);
	}

	internal sealed class FailOverLoadBalancer : ILoadBalancer
	{
		public static ILoadBalancer Instance { get; } = new FailOverLoadBalancer();

		public IEnumerable<string> LoadBalance(IReadOnlyList<string> hosts) => hosts;

		private FailOverLoadBalancer()
		{
		}
	}

	internal sealed class RandomLoadBalancer : ILoadBalancer
	{
		public static ILoadBalancer Instance { get; } = new RandomLoadBalancer();

		public IEnumerable<string> LoadBalance(IReadOnlyList<string> hosts)
		{
			// from https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle#The_modern_algorithm
			var shuffled = new List<string>(hosts);
			for (var i = hosts.Count - 1; i >= 1; i--)
			{
				int j;
				lock (m_random)
					j = m_random.Next(i + 1);
				if (i != j)
				{
					var temp = shuffled[i];
					shuffled[i] = shuffled[j];
					shuffled[j] = temp;
				}
			}
			return shuffled;
		}

		private RandomLoadBalancer() => m_random = new Random();

		readonly Random m_random;
	}

	internal sealed class RoundRobinLoadBalancer : ILoadBalancer
	{
		public RoundRobinLoadBalancer() => m_lock = new object();

		public IEnumerable<string> LoadBalance(IReadOnlyList<string> hosts)
		{
			int start;
			lock (m_lock)
				start = (int) (m_counter++ % hosts.Count);

			var shuffled = new List<string>(hosts.Count);
			for (var i = start; i < hosts.Count; i++)
				shuffled.Add(hosts[i]);
			for (var i = 0; i < start; i++)
				shuffled.Add(hosts[i]);
			return shuffled;
		}

		readonly object m_lock;
		uint m_counter;
	}
}
