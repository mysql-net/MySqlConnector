namespace MySqlConnector.Core;

internal interface ILoadBalancer
{
	/// <summary>
	/// Returns an <see cref="IEnumerable{String}"/> containing <paramref name="hosts"/> in the order they
	/// should be tried to satisfy the load balancing policy.
	/// </summary>
	IReadOnlyList<string> LoadBalance(IReadOnlyList<string> hosts);
}

internal sealed class FailOverLoadBalancer : ILoadBalancer
{
	public static ILoadBalancer Instance { get; } = new FailOverLoadBalancer();

	public IReadOnlyList<string> LoadBalance(IReadOnlyList<string> hosts) => hosts;

	private FailOverLoadBalancer()
	{
	}
}

internal sealed class RandomLoadBalancer : ILoadBalancer
{
	public static ILoadBalancer Instance { get; } = new RandomLoadBalancer();

	public IReadOnlyList<string> LoadBalance(IReadOnlyList<string> hosts)
	{
#pragma warning disable CA5394 // Do not use insecure randomness
#if NET8_0_OR_GREATER
		var shuffled = hosts.ToArray();
		lock (m_lock)
			m_random.Shuffle(shuffled);
		return shuffled;
#else
		var shuffled = new List<string>(hosts);
		// from https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle#The_modern_algorithm
		for (var i = hosts.Count - 1; i >= 1; i--)
		{
			int j;
			lock (m_lock)
				j = m_random.Next(i + 1);
			if (i != j)
			{
				var swap = shuffled[i];
				shuffled[i] = shuffled[j];
				shuffled[j] = swap;
			}
		}
		return shuffled;
#endif
	}

	private RandomLoadBalancer()
	{
		m_random = new();
		m_lock = new();
	}

	private readonly Random m_random;
#if NET9_0_OR_GREATER
	private readonly Lock m_lock;
#else
	private readonly object m_lock;
#endif
}

internal sealed class RoundRobinLoadBalancer : ILoadBalancer
{
	public RoundRobinLoadBalancer() => m_lock = new();

	public IReadOnlyList<string> LoadBalance(IReadOnlyList<string> hosts)
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

#if NET9_0_OR_GREATER
	private readonly Lock m_lock;
#else
	private readonly object m_lock;
#endif
	private uint m_counter;
}
