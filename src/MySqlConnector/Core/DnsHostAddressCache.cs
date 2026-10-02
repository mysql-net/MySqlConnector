using System.Net;
using MySqlConnector.Protocol.Serialization;
#if !NET6_0_OR_GREATER
using MySqlConnector.Utilities;
#endif

namespace MySqlConnector.Core;

internal sealed class DnsHostAddressCache
{
	public DnsHostAddressCache(IReadOnlyList<string> hostNames)
		: this(hostNames, ResolveHostAddressesAsync)
	{
	}

	internal DnsHostAddressCache(IReadOnlyList<string> hostNames, Func<string, IOBehavior, Task<IPAddress[]>> resolveHostAddressesAsync)
	{
		m_hostAddresses = new(hostNames.Count, StringComparer.OrdinalIgnoreCase);
		foreach (var hostName in hostNames)
			m_hostAddresses[hostName] = new();
		m_resolveHostAddressesAsync = resolveHostAddressesAsync;
	}

	public bool ContainsHostName(string hostName) => m_hostAddresses.ContainsKey(hostName);

	public async Task<IPAddress[]> GetHostAddressesAsync(string hostName, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// return quickly (without taking the lock) if the addresses have already been resolved
		var state = m_hostAddresses[hostName];
		return state.Addresses ?? await GetOrInitializeHostAddressesAsync(hostName, state, ioBehavior, cancellationToken).ConfigureAwait(false);
	}

	public async Task<(IPAddress[]? PreviousAddresses, IPAddress[] CurrentAddresses)> RefreshAsync(string hostName, IOBehavior ioBehavior)
	{
		var state = m_hostAddresses[hostName];

		IPAddress[]? previousAddresses;
		int version;
		lock (state.Lock)
			(previousAddresses, version) = (state.Addresses, state.Version);

		// if no previous addresses, perform a first-time initialization
		if (previousAddresses is null)
			return (null, await GetOrInitializeHostAddressesAsync(hostName, state, ioBehavior, CancellationToken.None).ConfigureAwait(false));

		// otherwise, check DNS for the current state and return both the previous and current addresses
		var currentAddresses = await m_resolveHostAddressesAsync(hostName, ioBehavior).ConfigureAwait(false);
		lock (state.Lock)
		{
			// if the cache was invalidated while resolving, new connections will resolve the host name again; don't overwrite their result with this older one
			if (state.Version != version)
				return (null, currentAddresses);

			previousAddresses = state.Addresses;
			state.Addresses = currentAddresses;
		}

		return (previousAddresses, currentAddresses);
	}

	public void Invalidate()
	{
		foreach (var state in m_hostAddresses.Values)
		{
			lock (state.Lock)
			{
				state.Addresses = null;

				// don't let new callers join a resolution that started before the cache was invalidated
				state.InitializationSource = null;
				state.Version++;
			}
		}
	}

	private async Task<IPAddress[]> GetOrInitializeHostAddressesAsync(string hostName, HostAddressState state, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		Task<IPAddress[]> sharedTask;
		TaskCompletionSource<IPAddress[]>? initializationSource = null;
		int version;
		lock (state.Lock)
		{
			if (state.Addresses is { } addresses)
				return addresses;

			version = state.Version;

			// first thread to take the lock will perform the initialization
			if (state.InitializationSource is null)
			{
				initializationSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
				state.InitializationSource = initializationSource;
			}
			sharedTask = state.InitializationSource.Task;
		}

		if (initializationSource is not null)
		{
			// resolve without this caller's cancellation token (because other callers may be waiting for the result)
			// discard the Task because it never throws (but propagates any exception to initializationSource)
			_ = InitializeHostAddressesAsync(hostName, state, initializationSource, version, ioBehavior);
		}
		else if (ioBehavior == IOBehavior.Synchronous)
		{
			// don't block this thread waiting for another caller's (possibly asynchronous) resolution; resolve the host name synchronously instead
			var resolvedAddresses = await m_resolveHostAddressesAsync(hostName, ioBehavior).ConfigureAwait(false);
			return PublishInitialAddresses(state, version, resolvedAddresses);
		}

		return await sharedTask.WaitAsync(cancellationToken).ConfigureAwait(false);
	}

	private async Task InitializeHostAddressesAsync(string hostName, HostAddressState state, TaskCompletionSource<IPAddress[]> initializationSource, int version, IOBehavior ioBehavior)
	{
		IPAddress[]? addresses = null;
		Exception? exception = null;
		try
		{
			addresses = await m_resolveHostAddressesAsync(hostName, ioBehavior).ConfigureAwait(false);
			addresses = PublishInitialAddresses(state, version, addresses);
		}
		catch (Exception ex)
		{
			exception = ex;
		}

		// no longer needed (unless the cache was invalidated and a new initialization has started); clear it before completing it
		// so that a caller that observes a failure can retry immediately
		lock (state.Lock)
		{
			if (state.InitializationSource == initializationSource)
				state.InitializationSource = null;
		}

		// copy the result or exception to all waiting callers
		if (exception is not null)
		{
			initializationSource.SetException(exception);

			// avoid UnobservedTaskException if every waiting caller was canceled before the failure
			_ = initializationSource.Task.Exception;
		}
		else
		{
			initializationSource.SetResult(addresses!);
		}
	}

	// Stores the first addresses resolved for a host (unless the cache was invalidated after the resolution started)
	// and returns the addresses the caller should use.
	private static IPAddress[] PublishInitialAddresses(HostAddressState state, int version, IPAddress[] addresses)
	{
		lock (state.Lock)
		{
			if (state.Version != version)
				return addresses;
			return state.Addresses ??= addresses;
		}
	}

	private static Task<IPAddress[]> ResolveHostAddressesAsync(string hostName, IOBehavior ioBehavior) =>
		ioBehavior == IOBehavior.Asynchronous ? Dns.GetHostAddressesAsync(hostName) : Task.FromResult(Dns.GetHostAddresses(hostName));

	private sealed class HostAddressState
	{
#if NET9_0_OR_GREATER
		public Lock Lock { get; } = new();
#else
		public object Lock { get; } = new();
#endif

		public IPAddress[]? Addresses { get; set; }

		public TaskCompletionSource<IPAddress[]>? InitializationSource { get; set; }

		public int Version { get; set; }
	}

	private readonly Dictionary<string, HostAddressState> m_hostAddresses;
	private readonly Func<string, IOBehavior, Task<IPAddress[]>> m_resolveHostAddressesAsync;
}
