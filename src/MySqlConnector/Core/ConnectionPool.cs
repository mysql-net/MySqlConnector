using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Security.Authentication;
using MySqlConnector.Logging;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core;

internal sealed class ConnectionPool : IDisposable
{
	public int Id { get; }

	public ConnectionSettings ConnectionSettings { get; }

	public SslProtocols SslProtocols { get; set; }

	public async ValueTask<ServerSession> GetSessionAsync(MySqlConnection connection, int startTickCount, int timeoutMilliseconds, Activity? activity, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// if all sessions are used, see if any have been leaked and can be recovered
		// check at most once per second (although this isn't enforced via a mutex so multiple threads might block
		// on the lock in RecoverLeakedSessions in high-concurrency situations
		if (IsEmpty && unchecked(((uint) Environment.TickCount) - m_lastRecoveryTime) >= 1000u)
		{
			Log.Debug("Pool{0} is empty; scanning for any leaked sessions", m_logArguments);
			await RecoverLeakedSessionsAsync(ioBehavior).ConfigureAwait(false);
		}

		if (ConnectionSettings.MinimumPoolSize > 0)
			await CreateMinimumPooledSessions(connection, ioBehavior, cancellationToken).ConfigureAwait(false);

		// wait for an open slot (until the cancellationToken is cancelled, which is typically due to timeout)
		Log.Trace("Pool{0} waiting for an available session", m_logArguments);
		if (ioBehavior == IOBehavior.Asynchronous)
			await m_sessionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
		else
			m_sessionSemaphore.Wait(cancellationToken);

		ServerSession? session = null;
		try
		{
			// check for a waiting session
			lock (m_sessions)
			{
				if (m_sessions.Count > 0)
				{
					session = m_sessions.First!.Value;
					m_sessions.RemoveFirst();
				}
			}
			if (session is not null)
			{
				Log.Trace("Pool{0} found an existing session; checking it for validity", m_logArguments);
				bool reuseSession;

				if (session.PoolGeneration != m_generation)
				{
					Log.Trace("Pool{0} discarding session due to wrong generation", m_logArguments);
					reuseSession = false;
				}
				else
				{
					if (ConnectionSettings.ConnectionReset || session.DatabaseOverride is not null)
					{
						if (timeoutMilliseconds != 0)
							session.SetTimeout(Math.Max(1, timeoutMilliseconds - (Environment.TickCount - startTickCount)));
						reuseSession = await session.TryResetConnectionAsync(ConnectionSettings, connection, ioBehavior, cancellationToken).ConfigureAwait(false);
						session.SetTimeout(Constants.InfiniteTimeout);
					}
					else
					{
						reuseSession = true;
					}
				}

				if (!reuseSession)
				{
					// session is either old or cannot communicate with the server
					Log.Info("Pool{0} Session{1} is unusable; destroying it", m_logArguments[0], session.Id);
					AdjustHostConnectionCount(session, -1);
					await session.DisposeAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
				}
				else
				{
					// pooled session is ready to be used; return it
					session.OwningConnection = new(connection);
					int leasedSessionsCountPooled;
					lock (m_leasedSessions)
					{
						m_leasedSessions.Add(session.Id, session);
						leasedSessionsCountPooled = m_leasedSessions.Count;
					}
					ActivitySourceHelper.CopyTags(session.ActivityTags, activity);
					if (Log.IsTraceEnabled())
						Log.Trace("Pool{0} returning pooled Session{1} to caller; LeasedSessionsCount={2}", m_logArguments[0], session.Id, leasedSessionsCountPooled);
					return session;
				}
			}

			// create a new session
			session = await ConnectSessionAsync(connection, "Pool{0} no pooled session available; created new Session{1}", startTickCount, activity, ioBehavior, cancellationToken).ConfigureAwait(false);
			AdjustHostConnectionCount(session, 1);
			session.OwningConnection = new(connection);
			int leasedSessionsCountNew;
			lock (m_leasedSessions)
			{
				m_leasedSessions.Add(session.Id, session);
				leasedSessionsCountNew = m_leasedSessions.Count;
			}
			if (Log.IsTraceEnabled())
				Log.Trace("Pool{0} returning new Session{1} to caller; LeasedSessionsCount={2}", m_logArguments[0], session.Id, leasedSessionsCountNew);
			return session;
		}
		catch (Exception ex)
		{
			if (session is not null)
			{
				try
				{
					Log.Debug(ex, "Pool{0} disposing created Session{1} due to exception: {2}", m_logArguments[0], session.Id, ex.Message);
					AdjustHostConnectionCount(session, -1);
					await session.DisposeAsync(ioBehavior, CancellationToken.None).ConfigureAwait(false);
				}
				catch (Exception unexpectedException)
				{
					Log.Warn(unexpectedException, "Pool{0} unexpected error in GetSessionAsync: {1}", m_logArguments[0], unexpectedException.Message);
				}
			}

			m_sessionSemaphore.Release();
			throw;
		}
	}

	/// <summary>
	/// Returns <c>true</c> if the connection pool is empty, i.e., all connections are in use. Note that in a highly-multithreaded
	/// environment, the value of this property may be stale by the time it's returned.
	/// </summary>
	internal bool IsEmpty => m_sessionSemaphore.CurrentCount == 0;

	// Returns zero for healthy, non-zero otherwise.
	private int GetSessionHealth(ServerSession session)
	{
		if (!session.IsConnected)
			return 1;
		if (session.PoolGeneration != m_generation)
			return 2;
		if (ConnectionSettings.ConnectionLifeTime > 0
			&& unchecked((uint) Environment.TickCount) - session.CreatedTicks >= ConnectionSettings.ConnectionLifeTime)
			return 3;

		return 0;
	}

	public async ValueTask ReturnAsync(IOBehavior ioBehavior, ServerSession session)
	{
		if (Log.IsTraceEnabled())
			Log.Trace("Pool{0} receiving Session{1} back", m_logArguments[0], session.Id);

		try
		{
			lock (m_leasedSessions)
				m_leasedSessions.Remove(session.Id);
			session.OwningConnection = null;
			var sessionHealth = GetSessionHealth(session);
			if (sessionHealth == 0)
			{
				lock (m_sessions)
					m_sessions.AddFirst(session);
			}
			else
			{
				if (sessionHealth == 1)
					Log.Info("Pool{0} received invalid Session{1}; destroying it", m_logArguments[0], session.Id);
				else
					Log.Debug("Pool{0} received expired Session{1}; destroying it", m_logArguments[0], session.Id);
				AdjustHostConnectionCount(session, -1);
				await session.DisposeAsync(ioBehavior, CancellationToken.None).ConfigureAwait(false);
			}
		}
		finally
		{
			m_sessionSemaphore.Release();
		}
	}

	public async Task ClearAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		// increment the generation of the connection pool
		Log.Info("Pool{0} clearing connection pool", m_logArguments);
		Interlocked.Increment(ref m_generation);
		m_procedureCache = null;
		await RecoverLeakedSessionsAsync(ioBehavior).ConfigureAwait(false);
		await CleanPoolAsync(ioBehavior, session => session.PoolGeneration != m_generation, false, cancellationToken).ConfigureAwait(false);
	}

	public async Task ReapAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		Log.Trace("Pool{0} reaping connection pool", m_logArguments);
		await RecoverLeakedSessionsAsync(ioBehavior).ConfigureAwait(false);
		await CleanPoolAsync(ioBehavior, session => (unchecked((uint) Environment.TickCount) - session.LastReturnedTicks) / 1000 >= ConnectionSettings.ConnectionIdleTimeout, true, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Returns the stored procedure cache for this <see cref="ConnectionPool"/>, lazily creating it on demand.
	/// This method may return a different object after <see cref="ClearAsync"/> has been called. The returned
	/// object is shared between multiple threads and is only safe to use after taking a <c>lock</c> on the
	/// object itself.
	/// </summary>
	public Dictionary<string, CachedProcedure?> GetProcedureCache()
	{
		var procedureCache = m_procedureCache;
		if (procedureCache is null)
		{
			var newProcedureCache = new Dictionary<string, CachedProcedure?>();
			procedureCache = Interlocked.CompareExchange(ref m_procedureCache, newProcedureCache, null) ?? newProcedureCache;
		}
		return procedureCache;
	}

	public void Dispose()
	{
		Log.Debug("Pool{0} disposing connection pool", m_logArguments);
#if NET6_0_OR_GREATER
		m_dnsCheckTimer?.Dispose();
		m_dnsCheckTimer = null;
		m_reaperTimer?.Dispose();
		m_reaperTimer = null;
#else
		if (m_dnsCheckTimer is not null)
		{
			using var dnsCheckWaitHandle = new ManualResetEvent(false);
			m_dnsCheckTimer.Dispose(dnsCheckWaitHandle);
			dnsCheckWaitHandle.WaitOne();
		}
		if (m_reaperTimer is not null)
		{
			using var reaperWaitHandle = new ManualResetEvent(false);
			m_reaperTimer.Dispose(reaperWaitHandle);
			reaperWaitHandle.WaitOne();
		}
#endif
	}

	/// <summary>
	/// Examines all the <see cref="ServerSession"/> objects in <see cref="m_leasedSessions"/> to determine if any
	/// have an owning <see cref="MySqlConnection"/> that has been garbage-collected. If so, assumes that the connection
	/// was not properly disposed and returns the session to the pool.
	/// </summary>
	private async Task RecoverLeakedSessionsAsync(IOBehavior ioBehavior)
	{
		var recoveredSessions = new List<(ServerSession Session, MySqlConnection Connection)>();
		lock (m_leasedSessions)
		{
			m_lastRecoveryTime = unchecked((uint) Environment.TickCount);
			foreach (var session in m_leasedSessions.Values)
			{
				if (!session.OwningConnection!.TryGetTarget(out var _))
				{
					// create a dummy MySqlConnection so that any thread running RecoverLeakedSessionsAsync doesn't process this one
					var connection = new MySqlConnection();
					session.OwningConnection = new(connection);
					recoveredSessions.Add((session, connection));
				}
			}
		}
		if (recoveredSessions.Count == 0)
			Log.Trace("Pool{0} recovered no sessions", m_logArguments);
		else
			Log.Warn("Pool{0}: RecoveredSessionCount={1}", m_logArguments[0], recoveredSessions.Count);

		foreach (var (session, connection) in recoveredSessions)
		{
			// bypass MySqlConnection.Dispose(Async), because it's a dummy MySqlConnection that's not set up
			// properly, and simply return the session to the pool directly
			await session.ReturnToPoolAsync(ioBehavior, null).ConfigureAwait(false);

			// be explicit about keeping the associated MySqlConnection alive until the session has been returned
			GC.KeepAlive(connection);
		}
	}

	private async Task CleanPoolAsync(IOBehavior ioBehavior, Func<ServerSession, bool> shouldCleanFn, bool respectMinPoolSize, CancellationToken cancellationToken)
	{
		// synchronize access to this method as only one clean routine should be run at a time
		if (ioBehavior == IOBehavior.Asynchronous)
			await m_cleanSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
		else
			m_cleanSemaphore.Wait(cancellationToken);

		try
		{
			var waitTimeout = TimeSpan.FromMilliseconds(10);
			while (true)
			{
				// if respectMinPoolSize is true, return if (leased sessions + waiting sessions <= minPoolSize)
				if (respectMinPoolSize)
				{
					lock (m_sessions)
					{
						if (ConnectionSettings.MaximumPoolSize - m_sessionSemaphore.CurrentCount + m_sessions.Count <= ConnectionSettings.MinimumPoolSize)
							return;
					}
				}

				// try to get an open slot; if this fails, connection pool is full and sessions will be disposed when returned to pool
				if (ioBehavior == IOBehavior.Asynchronous)
				{
					if (!await m_sessionSemaphore.WaitAsync(waitTimeout, cancellationToken).ConfigureAwait(false))
						return;
				}
				else
				{
					if (!m_sessionSemaphore.Wait(waitTimeout, cancellationToken))
						return;
				}

				try
				{
					// check for a waiting session
					ServerSession? session = null;
					lock (m_sessions)
					{
						if (m_sessions.Count > 0)
						{
							session = m_sessions.Last!.Value;
							m_sessions.RemoveLast();
						}
					}
					if (session is null)
						return;

					if (shouldCleanFn(session))
					{
						// session should be cleaned; dispose it and keep iterating
						Log.Debug("Pool{0} found Session{1} to clean up", m_logArguments[0], session.Id);
						await session.DisposeAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
					}
					else
					{
						// session should not be cleaned; put it back in the queue and stop iterating
						lock (m_sessions)
							m_sessions.AddLast(session);
						return;
					}
				}
				finally
				{
					m_sessionSemaphore.Release();
				}
			}
		}
		finally
		{
			m_cleanSemaphore.Release();
		}
	}

	private async Task CreateMinimumPooledSessions(MySqlConnection connection, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		while (true)
		{
			lock (m_sessions)
			{
				// check if the desired minimum number of sessions have been created
				if (ConnectionSettings.MaximumPoolSize - m_sessionSemaphore.CurrentCount + m_sessions.Count >= ConnectionSettings.MinimumPoolSize)
					return;
			}

			// acquire the semaphore, to ensure that the maximum number of sessions isn't exceeded; if it can't be acquired,
			// we have reached the maximum number of sessions and no more need to be created
			if (ioBehavior == IOBehavior.Asynchronous)
			{
				if (!await m_sessionSemaphore.WaitAsync(0, cancellationToken).ConfigureAwait(false))
					return;
			}
			else
			{
				if (!m_sessionSemaphore.Wait(0, cancellationToken))
					return;
			}

			try
			{
				var session = await ConnectSessionAsync(connection, "Pool{0} created Session{1} to reach minimum pool size", Environment.TickCount, null, ioBehavior, cancellationToken).ConfigureAwait(false);
				AdjustHostConnectionCount(session, 1);
				lock (m_sessions)
					m_sessions.AddFirst(session);
			}
			finally
			{
				// connection is in pool; semaphore shouldn't be held any more
				m_sessionSemaphore.Release();
			}
		}
	}

	private async ValueTask<ServerSession> ConnectSessionAsync(MySqlConnection connection, string logMessage, int startTickCount, Activity? activity, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		var session = new ServerSession(this, m_generation, Interlocked.Increment(ref m_lastSessionId));
		if (Log.IsDebugEnabled())
			Log.Debug(logMessage, m_logArguments[0], session.Id);
		string? statusInfo;
		try
		{
			statusInfo = await session.ConnectAsync(ConnectionSettings, connection, startTickCount, m_loadBalancer, activity, ioBehavior, cancellationToken).ConfigureAwait(false);
		}
		catch (Exception)
		{
			await session.DisposeAsync(ioBehavior, default).ConfigureAwait(false);
			throw;
		}

		Exception? redirectionException = null;
		if (statusInfo is not null && statusInfo.StartsWith("Location: mysql://", StringComparison.Ordinal))
		{
			// server redirection string has the format "Location: mysql://{host}:{port}/user={userId}[&ttl={ttl}]"
			Log.Trace("Session{0} has server redirection header {1}", session.Id, statusInfo);

			if (ConnectionSettings.ServerRedirectionMode == MySqlServerRedirectionMode.Disabled)
			{
				Log.Trace("Pool{0} server redirection is disabled; ignoring redirection", m_logArguments);
			}
			else if (Utility.TryParseRedirectionHeader(statusInfo, out var host, out var port, out var user))
			{
				if (host != ConnectionSettings.HostNames![0] || port != ConnectionSettings.Port || user != ConnectionSettings.UserID)
				{
					var redirectedSettings = ConnectionSettings.CloneWith(host, port, user);
					Log.Debug("Pool{0} opening new connection to Host={1}; Port={2}; User={3}", m_logArguments[0], host, port, user);
					var redirectedSession = new ServerSession(this, m_generation, Interlocked.Increment(ref m_lastSessionId));
					try
					{
						await redirectedSession.ConnectAsync(redirectedSettings, connection, startTickCount, m_loadBalancer, activity, ioBehavior, cancellationToken).ConfigureAwait(false);
					}
					catch (Exception ex)
					{
						Log.Info(ex, "Pool{0} failed to connect redirected Session{1}", m_logArguments[0], redirectedSession.Id);
						redirectionException = ex;
					}

					if (redirectionException is null)
					{
						Log.Trace("Pool{0} closing Session{1} to use redirected Session{2} instead", m_logArguments[0], session.Id, redirectedSession.Id);
						await session.DisposeAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
						return redirectedSession;
					}
					else
					{
						try
						{
							await redirectedSession.DisposeAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
						}
						catch (Exception)
						{
						}
					}
				}
				else
				{
					Log.Trace("Session{0} is already connected to this server; ignoring redirection", session.Id);
				}
			}
		}

		if (ConnectionSettings.ServerRedirectionMode == MySqlServerRedirectionMode.Required)
		{
			Log.Error("Pool{0} requires server redirection but server doesn't support it", m_logArguments);
			throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, "Server does not support redirection", redirectionException);
		}
		return session;
	}

	public static ConnectionPool? CreatePool(string connectionString)
	{
		// parse connection string and check for 'Pooling' setting; return 'null' if pooling is disabled
		var connectionStringBuilder = new MySqlConnectionStringBuilder(connectionString);
		if (!connectionStringBuilder.Pooling)
		{
			return null;
		}

		// force a new pool to be created, ignoring the cache
		var connectionSettings = new ConnectionSettings(connectionStringBuilder);
		var pool = new ConnectionPool(connectionSettings);
		pool.StartReaperTask();
		pool.StartDnsCheckTimer();
		return pool;
	}

	public static ConnectionPool? GetPool(string connectionString)
	{
		// check single-entry MRU cache for this exact connection string; most applications have just one
		// connection string and will get a cache hit here
		var cache = s_mruCache;
		if (cache?.ConnectionString == connectionString)
			return cache.Pool;

		// check if pool has already been created for this exact connection string
		if (s_pools.TryGetValue(connectionString, out var pool))
		{
			s_mruCache = new(connectionString, pool);
			return pool;
		}

		// parse connection string and check for 'Pooling' setting; return 'null' if pooling is disabled
		var connectionStringBuilder = new MySqlConnectionStringBuilder(connectionString);
		if (!connectionStringBuilder.Pooling)
		{
			s_pools.GetOrAdd(connectionString, default(ConnectionPool));
			s_mruCache = new(connectionString, null);
			return null;
		}

		// check for pool using normalized form of connection string
		var normalizedConnectionString = connectionStringBuilder.ConnectionString;
		if (normalizedConnectionString != connectionString && s_pools.TryGetValue(normalizedConnectionString, out pool))
		{
			// try to set the pool for the connection string to the canonical pool; if someone else
			// beats us to it, just use the existing value
			pool = s_pools.GetOrAdd(connectionString, pool)!;
			s_mruCache = new(connectionString, pool);
			return pool;
		}

		// create a new pool and attempt to insert it; if someone else beats us to it, just use their value
		var connectionSettings = new ConnectionSettings(connectionStringBuilder);
		var newPool = new ConnectionPool(connectionSettings);
		pool = s_pools.GetOrAdd(normalizedConnectionString, newPool);

		if (pool == newPool)
		{
			s_mruCache = new(connectionString, pool);
			pool.StartReaperTask();
			pool.StartDnsCheckTimer();

			// if we won the race to create the new pool, also store it under the original connection string
			if (connectionString != normalizedConnectionString)
				s_pools.GetOrAdd(connectionString, pool);
		}
		else if (pool != newPool && Log.IsDebugEnabled())
		{
			Log.Debug("Pool{0} was created but will not be used (due to race)", newPool.m_logArguments);
		}

		return pool;
	}

	public static async Task ClearPoolsAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		foreach (var pool in GetAllPools())
			await pool.ClearAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
	}

	private static IReadOnlyList<ConnectionPool> GetAllPools()
	{
		var pools = new List<ConnectionPool>(s_pools.Count);
		var uniquePools = new HashSet<ConnectionPool>();
		foreach (var pool in s_pools.Values)
		{
			if (pool is not null && uniquePools.Add(pool))
				pools.Add(pool);
		}
		return pools;
	}

	private ConnectionPool(ConnectionSettings cs)
	{
		ConnectionSettings = cs;
		SslProtocols = cs.TlsVersions;
		m_generation = 0;
		m_cleanSemaphore = new(1);
		m_sessionSemaphore = new(cs.MaximumPoolSize);
		m_sessions = new();
		m_leasedSessions = new();
		if (cs.ConnectionProtocol == MySqlConnectionProtocol.Sockets && cs.LoadBalance == MySqlLoadBalance.LeastConnections)
		{
			m_hostSessions = new();
			foreach (var hostName in cs.HostNames!)
				m_hostSessions[hostName] = 0;
		}

		m_loadBalancer = cs.ConnectionProtocol != MySqlConnectionProtocol.Sockets ? null :
			cs.HostNames!.Count == 1 || cs.LoadBalance == MySqlLoadBalance.FailOver ? FailOverLoadBalancer.Instance :
			cs.LoadBalance == MySqlLoadBalance.Random ? RandomLoadBalancer.Instance :
			cs.LoadBalance == MySqlLoadBalance.LeastConnections ? new LeastConnectionsLoadBalancer(m_hostSessions!) :
			(ILoadBalancer) new RoundRobinLoadBalancer();

		Id = Interlocked.Increment(ref s_poolId);
		m_logArguments = new object[] { Id.ToString(CultureInfo.InvariantCulture) };
		if (Log.IsInfoEnabled())
			Log.Info("Pool{0} creating new connection pool for ConnectionString: {1}", m_logArguments[0], cs.ConnectionStringBuilder.GetConnectionString(includePassword: false));
	}

	private void StartReaperTask()
	{
		if (ConnectionSettings.ConnectionIdleTimeout <= 0)
			return;

		var reaperInterval = TimeSpan.FromSeconds(Math.Max(1, Math.Min(60, ConnectionSettings.ConnectionIdleTimeout / 2)));

#if NET6_0_OR_GREATER
		m_reaperTimer = new PeriodicTimer(reaperInterval);
		_ = RunTimer();

		async Task RunTimer()
		{
			while (await m_reaperTimer.WaitForNextTickAsync().ConfigureAwait(false))
			{
				try
				{
					using var source = new CancellationTokenSource(reaperInterval);
					await ReapAsync(IOBehavior.Asynchronous, source.Token).ConfigureAwait(false);
				}
				catch
				{
					// do nothing; we'll try to reap again
				}
			}
		}
#else
		m_reaperTimer = new Timer(t =>
		{
			var stopwatch = Stopwatch.StartNew();
			try
			{
				using var source = new CancellationTokenSource(reaperInterval);
				ReapAsync(IOBehavior.Synchronous, source.Token).GetAwaiter().GetResult();
			}
			catch
			{
				// do nothing; we'll try to reap again
			}

			// restart the timer, accounting for the time spent reaping
			var delay = reaperInterval - stopwatch.Elapsed;
			((Timer) t!).Change(delay < TimeSpan.Zero ? TimeSpan.Zero : delay, TimeSpan.FromMilliseconds(-1));
		});
		m_reaperTimer.Change(reaperInterval, TimeSpan.FromMilliseconds(-1));
#endif
	}

	private void StartDnsCheckTimer()
	{
		if (ConnectionSettings.ConnectionProtocol != MySqlConnectionProtocol.Tcp || ConnectionSettings.DnsCheckInterval <= 0)
			return;

		var hostNames = ConnectionSettings.HostNames!;
		var hostAddresses = new IPAddress[hostNames.Count][];

#if NET6_0_OR_GREATER
		m_dnsCheckTimer = new PeriodicTimer(TimeSpan.FromSeconds(ConnectionSettings.DnsCheckInterval));
		_ = RunTimer();

		async Task RunTimer()
		{
			while (await m_dnsCheckTimer.WaitForNextTickAsync().ConfigureAwait(false))
			{
				Log.Trace("Pool{0} checking for DNS changes", m_logArguments);
				var hostNamesChanged = false;
				for (var hostNameIndex = 0; hostNameIndex < hostNames.Count; hostNameIndex++)
				{
					try
					{
						var ipAddresses = await Dns.GetHostAddressesAsync(hostNames[hostNameIndex]).ConfigureAwait(false);
						if (hostAddresses[hostNameIndex] is null)
						{
							hostAddresses[hostNameIndex] = ipAddresses;
						}
						else if (hostAddresses[hostNameIndex].Except(ipAddresses).Any())
						{
							Log.Debug("Pool{0} detected DNS change for HostName '{1}': {2} to {3}", m_logArguments[0], hostNames[hostNameIndex], string.Join<IPAddress>(',', hostAddresses[hostNameIndex]), string.Join<IPAddress>(',', ipAddresses));
							hostAddresses[hostNameIndex] = ipAddresses;
							hostNamesChanged = true;
						}
					}
					catch (Exception ex)
					{
						// do nothing; we'll try again later
						Log.Debug("Pool{0} DNS check failed; ignoring HostName '{1}': {2}", m_logArguments[0], hostNames[hostNameIndex], ex.Message);
					}
				}
				if (hostNamesChanged)
				{
					Log.Info("Pool{0} clearing pool due to DNS changes", m_logArguments);
					await ClearAsync(IOBehavior.Asynchronous, CancellationToken.None).ConfigureAwait(false);
				}
			}
		}
#else
		var interval = Math.Min(int.MaxValue / 1000, ConnectionSettings.DnsCheckInterval) * 1000;
		m_dnsCheckTimer = new Timer(t =>
		{
			Log.Trace("Pool{0} checking for DNS changes", m_logArguments);
			var hostNamesChanged = false;
			for (var hostNameIndex = 0; hostNameIndex < hostNames.Count; hostNameIndex++)
			{
				try
				{
					var ipAddresses = Dns.GetHostAddresses(hostNames[hostNameIndex]);
					if (hostAddresses[hostNameIndex] is null)
					{
						hostAddresses[hostNameIndex] = ipAddresses;
					}
					else if (hostAddresses[hostNameIndex].Except(ipAddresses).Any())
					{
						Log.Debug("Pool{0} detected DNS change for HostName '{1}': {2} to {3}", m_logArguments[0], hostNames[hostNameIndex], string.Join<IPAddress>(",", hostAddresses[hostNameIndex]), string.Join<IPAddress>(",", ipAddresses));
						hostAddresses[hostNameIndex] = ipAddresses;
						hostNamesChanged = true;
					}
				}
				catch (Exception ex)
				{
					// do nothing; we'll try again later
					Log.Debug("Pool{0} DNS check failed; ignoring HostName '{1}': {2}", m_logArguments[0], hostNames[hostNameIndex], ex.Message);
				}
			}
			if (hostNamesChanged)
			{
				Log.Info("Pool{0} clearing pool due to DNS changes", m_logArguments);
				ClearAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
			}
			((Timer) t!).Change(interval, -1);
		});
		m_dnsCheckTimer.Change(interval, -1);
#endif
	}

	private void AdjustHostConnectionCount(ServerSession session, int delta)
	{
		if (m_hostSessions is not null)
		{
			lock (m_hostSessions)
				m_hostSessions[session.HostName] += delta;
		}
	}

	private sealed class LeastConnectionsLoadBalancer : ILoadBalancer
	{
		public LeastConnectionsLoadBalancer(Dictionary<string, int> hostSessions) => m_hostSessions = hostSessions;

		public IReadOnlyList<string> LoadBalance(IReadOnlyList<string> hosts)
		{
			lock (m_hostSessions)
				return m_hostSessions.OrderBy(static x => x.Value).Select(static x => x.Key).ToList();
		}

		private readonly Dictionary<string, int> m_hostSessions;
	}

	private sealed class ConnectionStringPool
	{
		public ConnectionStringPool(string connectionString, ConnectionPool? pool)
		{
			ConnectionString = connectionString;
			Pool = pool;
		}

		public string ConnectionString { get; }
		public ConnectionPool? Pool { get; }
	}

	static ConnectionPool()
	{
		AppDomain.CurrentDomain.DomainUnload += OnAppDomainShutDown;
		AppDomain.CurrentDomain.ProcessExit += OnAppDomainShutDown;
	}

	private static void OnAppDomainShutDown(object? sender, EventArgs e) =>
		ClearPoolsAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();

	private static readonly IMySqlConnectorLogger Log = MySqlConnectorLogManager.CreateLogger(nameof(ConnectionPool));
	private static readonly ConcurrentDictionary<string, ConnectionPool?> s_pools = new();

	private static int s_poolId;
	private static ConnectionStringPool? s_mruCache;

	private readonly SemaphoreSlim m_cleanSemaphore;
	private readonly SemaphoreSlim m_sessionSemaphore;
	private readonly LinkedList<ServerSession> m_sessions;
	private readonly Dictionary<string, ServerSession> m_leasedSessions;
	private readonly ILoadBalancer? m_loadBalancer;
	private readonly Dictionary<string, int>? m_hostSessions;
	private readonly object[] m_logArguments;
	private int m_generation;
	private uint m_lastRecoveryTime;
	private int m_lastSessionId;
	private Dictionary<string, CachedProcedure?>? m_procedureCache;
#if NET6_0_OR_GREATER
	private PeriodicTimer? m_dnsCheckTimer;
	private PeriodicTimer? m_reaperTimer;
#else
	private Timer? m_dnsCheckTimer;
	private Timer? m_reaperTimer;
#endif
}
