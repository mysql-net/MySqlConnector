using System.Net;
using System.Net.Sockets;
using MySqlConnector.Core;
using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Tests;

public class DnsHostAddressCacheTests
{
	[Fact]
	public async Task ReusesInitialResolution()
	{
		var resolutionCount = 0;
		var address = IPAddress.Parse("192.0.2.1");
		var cache = new DnsHostAddressCache(["example.test"], (_, _) =>
		{
			Interlocked.Increment(ref resolutionCount);
			return Task.FromResult(new[] { address });
		});

		var firstAddresses = await cache.GetHostAddressesAsync("example.test", IOBehavior.Asynchronous, CancellationToken.None);
		var secondAddresses = await cache.GetHostAddressesAsync("example.test", IOBehavior.Asynchronous, CancellationToken.None);

		Assert.Same(firstAddresses, secondAddresses);
		Assert.Equal(1, resolutionCount);
	}

	[Fact]
	public async Task AllowsDuplicateHostNames()
	{
		var resolutionCount = 0;
		var address = IPAddress.Parse("192.0.2.1");
		const string hostname = "example.test";
		var cache = new DnsHostAddressCache([hostname, hostname], (_, _) =>
		{
			Interlocked.Increment(ref resolutionCount);
			return Task.FromResult(new[] { address });
		});

		Assert.True(cache.ContainsHostName(hostname));

		var addresses = await cache.GetHostAddressesAsync(hostname, IOBehavior.Asynchronous, CancellationToken.None);
		Assert.Equal([address], addresses);
	}

	[Fact]
	public async Task RefreshPublishesAddressesForNewConnections()
	{
		var addresses = new Queue<IPAddress[]>([
			[IPAddress.Parse("192.0.2.1")],
			[IPAddress.Parse("192.0.2.2")],
		]);
		var cache = new DnsHostAddressCache(["example.test"], (_, _) => Task.FromResult(addresses.Dequeue()));
		var initialAddresses = await cache.GetHostAddressesAsync("example.test", IOBehavior.Asynchronous, CancellationToken.None);

		var (previousAddresses, currentAddresses) = await cache.RefreshAsync("example.test", IOBehavior.Asynchronous);
		var connectionAddresses = await cache.GetHostAddressesAsync("example.test", IOBehavior.Asynchronous, CancellationToken.None);

		Assert.Same(initialAddresses, previousAddresses);
		Assert.Equal(IPAddress.Parse("192.0.2.2"), Assert.Single(currentAddresses));
		Assert.Same(currentAddresses, connectionAddresses);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task InitialRefreshAndConnectionShareResolution(bool connectionFirst)
	{
		var resolutionSource = new TaskCompletionSource<IPAddress[]>(TaskCreationOptions.RunContinuationsAsynchronously);
		var resolutionCount = 0;
		var cache = new DnsHostAddressCache(["example.test"], (_, _) =>
		{
			Interlocked.Increment(ref resolutionCount);
			return resolutionSource.Task;
		});

		var connectionTask = connectionFirst ? cache.GetHostAddressesAsync("example.test", IOBehavior.Asynchronous, CancellationToken.None) : null;
		var refreshTask = cache.RefreshAsync("example.test", IOBehavior.Asynchronous);
		connectionTask ??= cache.GetHostAddressesAsync("example.test", IOBehavior.Asynchronous, CancellationToken.None);
		var address = IPAddress.Parse("192.0.2.1");
		resolutionSource.SetResult([address]);

		var (previousAddresses, currentAddresses) = await refreshTask;
		var connectionAddresses = await connectionTask;

		Assert.Null(previousAddresses);
		Assert.Same(currentAddresses, connectionAddresses);
		Assert.Equal(1, resolutionCount);
	}

	[Fact]
	public async Task CancelingCallerDoesNotCancelSharedResolution()
	{
		var resolutionSource = new TaskCompletionSource<IPAddress[]>(TaskCreationOptions.RunContinuationsAsynchronously);
		var resolutionCount = 0;
		var cache = new DnsHostAddressCache(["example.test"], (_, _) =>
		{
			Interlocked.Increment(ref resolutionCount);
			return resolutionSource.Task;
		});
		using var cancellationSource = new CancellationTokenSource();
		var canceledTask = cache.GetHostAddressesAsync("example.test", IOBehavior.Asynchronous, cancellationSource.Token);
		var connectionTask = cache.GetHostAddressesAsync("example.test", IOBehavior.Asynchronous, CancellationToken.None);

		cancellationSource.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledTask);
		Assert.False(connectionTask.IsCompleted);
		resolutionSource.SetResult([IPAddress.Parse("192.0.2.1")]);

		var addresses = await connectionTask;
		Assert.Equal(IPAddress.Parse("192.0.2.1"), Assert.Single(addresses));
		Assert.Equal(1, resolutionCount);
	}

	[Fact]
	public async Task FailedInitialResolutionCanBeRetried()
	{
		var resolutionCount = 0;
		var cache = new DnsHostAddressCache(["example.test"], (_, _) =>
		{
			if (Interlocked.Increment(ref resolutionCount) == 1)
				return Task.FromException<IPAddress[]>(new SocketException());
			return Task.FromResult(new[] { IPAddress.Parse("192.0.2.1") });
		});

		await Assert.ThrowsAsync<SocketException>(() => cache.GetHostAddressesAsync("example.test", IOBehavior.Asynchronous, CancellationToken.None));
		var addresses = await cache.GetHostAddressesAsync("example.test", IOBehavior.Asynchronous, CancellationToken.None);

		Assert.Equal(IPAddress.Parse("192.0.2.1"), Assert.Single(addresses));
		Assert.Equal(2, resolutionCount);
	}

	[Fact]
	public async Task FailedRefreshKeepsLastKnownAddresses()
	{
		var resolutionCount = 0;
		var cache = new DnsHostAddressCache(["example.test"], (_, _) =>
		{
			switch (Interlocked.Increment(ref resolutionCount))
			{
				case 1:
					return Task.FromResult(new[] { IPAddress.Parse("192.0.2.1") });
				case 2:
					return Task.FromException<IPAddress[]>(new SocketException());
				default:
					return Task.FromResult(new[] { IPAddress.Parse("192.0.2.2") });
			}
		});
		var initialAddresses = await cache.GetHostAddressesAsync("example.test", IOBehavior.Asynchronous, CancellationToken.None);

		await Assert.ThrowsAsync<SocketException>(() => cache.RefreshAsync("example.test", IOBehavior.Asynchronous));
		var addressesAfterFailure = await cache.GetHostAddressesAsync("example.test", IOBehavior.Asynchronous, CancellationToken.None);
		var (previousAddresses, currentAddresses) = await cache.RefreshAsync("example.test", IOBehavior.Asynchronous);

		Assert.Same(initialAddresses, addressesAfterFailure);
		Assert.Same(initialAddresses, previousAddresses);
		Assert.Equal(IPAddress.Parse("192.0.2.2"), Assert.Single(currentAddresses));
	}

	[Fact]
	public async Task InvalidateResolvesHostNameAgain()
	{
		var resolutionCount = 0;
		var cache = new DnsHostAddressCache(["example.test"], (_, _) =>
			Task.FromResult(new[] { IPAddress.Parse(Interlocked.Increment(ref resolutionCount) == 1 ? "192.0.2.1" : "192.0.2.2") }));
		var initialAddresses = await cache.GetHostAddressesAsync("example.test", IOBehavior.Asynchronous, CancellationToken.None);

		cache.Invalidate();
		var addresses = await cache.GetHostAddressesAsync("example.test", IOBehavior.Asynchronous, CancellationToken.None);
		var (previousAddresses, currentAddresses) = await cache.RefreshAsync("example.test", IOBehavior.Asynchronous);

		Assert.Equal(IPAddress.Parse("192.0.2.1"), Assert.Single(initialAddresses));
		Assert.Equal(IPAddress.Parse("192.0.2.2"), Assert.Single(addresses));
		Assert.Same(addresses, previousAddresses);
		Assert.Equal(IPAddress.Parse("192.0.2.2"), Assert.Single(currentAddresses));
		Assert.Equal(3, resolutionCount);
	}

	[Fact]
	public async Task InitialRefreshAfterInvalidateDoesNotReportChange()
	{
		var resolutionCount = 0;
		var cache = new DnsHostAddressCache(["example.test"], (_, _) =>
			Task.FromResult(new[] { IPAddress.Parse(Interlocked.Increment(ref resolutionCount) == 1 ? "192.0.2.1" : "192.0.2.2") }));
		await cache.GetHostAddressesAsync("example.test", IOBehavior.Asynchronous, CancellationToken.None);

		cache.Invalidate();
		var (previousAddresses, currentAddresses) = await cache.RefreshAsync("example.test", IOBehavior.Asynchronous);
		var connectionAddresses = await cache.GetHostAddressesAsync("example.test", IOBehavior.Asynchronous, CancellationToken.None);

		Assert.Null(previousAddresses);
		Assert.Same(currentAddresses, connectionAddresses);
		Assert.Equal(IPAddress.Parse("192.0.2.2"), Assert.Single(connectionAddresses));
	}

	[Fact]
	public async Task RefreshStartedBeforeInvalidateDoesNotOverwriteNewAddresses()
	{
		var refreshSource = new TaskCompletionSource<IPAddress[]>(TaskCreationOptions.RunContinuationsAsynchronously);
		var resolutionCount = 0;
		var cache = new DnsHostAddressCache(["example.test"], (_, _) => Interlocked.Increment(ref resolutionCount) switch
		{
			1 => Task.FromResult(new[] { IPAddress.Parse("192.0.2.1") }),
			2 => refreshSource.Task,
			_ => Task.FromResult(new[] { IPAddress.Parse("192.0.2.3") }),
		});
		await cache.GetHostAddressesAsync("example.test", IOBehavior.Asynchronous, CancellationToken.None);
		var refreshTask = cache.RefreshAsync("example.test", IOBehavior.Asynchronous);

		cache.Invalidate();
		var newAddresses = await cache.GetHostAddressesAsync("example.test", IOBehavior.Asynchronous, CancellationToken.None);
		refreshSource.SetResult([IPAddress.Parse("192.0.2.2")]);
		var (previousAddresses, _) = await refreshTask;
		var connectionAddresses = await cache.GetHostAddressesAsync("example.test", IOBehavior.Asynchronous, CancellationToken.None);

		Assert.Null(previousAddresses);
		Assert.Equal(IPAddress.Parse("192.0.2.3"), Assert.Single(newAddresses));
		Assert.Same(newAddresses, connectionAddresses);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task InitializationStartedBeforeInvalidateIsNotShared(bool completeFirstInitializationFirst)
	{
		var firstSource = new TaskCompletionSource<IPAddress[]>(TaskCreationOptions.RunContinuationsAsynchronously);
		var secondSource = new TaskCompletionSource<IPAddress[]>(TaskCreationOptions.RunContinuationsAsynchronously);
		var resolutionCount = 0;
		var cache = new DnsHostAddressCache(["example.test"], (_, _) =>
			Interlocked.Increment(ref resolutionCount) == 1 ? firstSource.Task : secondSource.Task);
		var firstTask = cache.GetHostAddressesAsync("example.test", IOBehavior.Asynchronous, CancellationToken.None);

		cache.Invalidate();
		var secondTask = cache.GetHostAddressesAsync("example.test", IOBehavior.Asynchronous, CancellationToken.None);
		if (completeFirstInitializationFirst)
			firstSource.SetResult([IPAddress.Parse("192.0.2.1")]);
		secondSource.SetResult([IPAddress.Parse("192.0.2.2")]);
		if (!completeFirstInitializationFirst)
			firstSource.SetResult([IPAddress.Parse("192.0.2.1")]);
		var firstAddresses = await firstTask;
		var secondAddresses = await secondTask;
		var connectionAddresses = await cache.GetHostAddressesAsync("example.test", IOBehavior.Asynchronous, CancellationToken.None);

		Assert.Equal(IPAddress.Parse("192.0.2.1"), Assert.Single(firstAddresses));
		Assert.Equal(IPAddress.Parse("192.0.2.2"), Assert.Single(secondAddresses));
		Assert.Same(secondAddresses, connectionAddresses);
		Assert.Equal(2, resolutionCount);
	}

	[Fact]
	public async Task SynchronousCallerDoesNotWaitForAsynchronousInitialization()
	{
		var asynchronousSource = new TaskCompletionSource<IPAddress[]>(TaskCreationOptions.RunContinuationsAsynchronously);
		var cache = new DnsHostAddressCache(["example.test"], (_, ioBehavior) =>
			ioBehavior == IOBehavior.Asynchronous ? asynchronousSource.Task : Task.FromResult(new[] { IPAddress.Parse("192.0.2.2") }));
		var asynchronousTask = cache.GetHostAddressesAsync("example.test", IOBehavior.Asynchronous, CancellationToken.None);

		var synchronousTask = cache.GetHostAddressesAsync("example.test", IOBehavior.Synchronous, CancellationToken.None);
		Assert.True(synchronousTask.IsCompleted);
		asynchronousSource.SetResult([IPAddress.Parse("192.0.2.1")]);
		var synchronousAddresses = await synchronousTask;
		var asynchronousAddresses = await asynchronousTask;

		// all callers use the first addresses that were resolved
		Assert.Equal(IPAddress.Parse("192.0.2.2"), Assert.Single(synchronousAddresses));
		Assert.Same(synchronousAddresses, asynchronousAddresses);
	}
}
