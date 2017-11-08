using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using MySqlConnector.Logging;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core
{
	internal sealed class ConnectionPool
	{
		public int Id { get; }

		public async Task<ServerSession> GetSessionAsync(MySqlConnection connection, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			// if all sessions are used, see if any have been leaked and can be recovered
			// check at most once per second (although this isn't enforced via a mutex so multiple threads might block
			// on the lock in RecoverLeakedSessions in high-concurrency situations
			if (m_sessionSemaphore.CurrentCount == 0 && unchecked(((uint) Environment.TickCount) - m_lastRecoveryTime) >= 1000u)
			{
				Log.Warn("{0} is empty; recovering leaked sessions", m_logArguments);
				RecoverLeakedSessions();
			}

			if (m_connectionSettings.MinimumPoolSize > 0)
				await CreateMinimumPooledSessions(ioBehavior, cancellationToken).ConfigureAwait(false);

			// wait for an open slot (until the cancellationToken is cancelled, which is typically due to timeout)
			Log.Debug("{0} waiting for an available session", m_logArguments);
			if (ioBehavior == IOBehavior.Asynchronous)
				await m_sessionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			else
				m_sessionSemaphore.Wait(cancellationToken);

			try
			{
				// check for a waiting session
				ServerSession session = null;
				lock (m_sessions)
				{
					if (m_sessions.Count > 0)
					{
						session = m_sessions.First.Value;
						m_sessions.RemoveFirst();
					}
				}
				if (session != null)
				{
					Log.Debug("{0} found an existing session; checking it for validity", m_logArguments);
					bool reuseSession;

					if (session.PoolGeneration != m_generation)
					{
						Log.Debug("{0} discarding session due to wrong generation", m_logArguments);
						reuseSession = false;
					}
					else
					{
						if (m_connectionSettings.ConnectionReset)
						{
							reuseSession = await session.TryResetConnectionAsync(m_connectionSettings, ioBehavior, cancellationToken).ConfigureAwait(false);
						}
						else
						{
							reuseSession = await session.TryPingAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
						}
					}

					if (!reuseSession)
					{
						// session is either old or cannot communicate with the server
						Log.Warn("{0} Session{1} is unusable; destroying it", m_logArguments[0], session.Id);
						AdjustHostConnectionCount(session, -1);
						await session.DisposeAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
					}
					else
					{
						// pooled session is ready to be used; return it
						session.OwningConnection = new WeakReference<MySqlConnection>(connection);
						int leasedSessionsCountPooled;
						lock (m_leasedSessions)
						{
							m_leasedSessions.Add(session.Id, session);
							leasedSessionsCountPooled = m_leasedSessions.Count;
						}
						if (Log.IsDebugEnabled())
							Log.Debug("{0} returning pooled Session{1} to caller; m_leasedSessions.Count={2}", m_logArguments[0], session.Id, leasedSessionsCountPooled);
						return session;
					}
				}

				// create a new session
				session = new ServerSession(this, m_generation, Interlocked.Increment(ref m_lastSessionId));
				if (Log.IsInfoEnabled())
					Log.Info("{0} no pooled session available; created new Session{1}", m_logArguments[0], session.Id);
				await session.ConnectAsync(m_connectionSettings, m_loadBalancer, ioBehavior, cancellationToken).ConfigureAwait(false);
				AdjustHostConnectionCount(session, 1);
				session.OwningConnection = new WeakReference<MySqlConnection>(connection);
				int leasedSessionsCountNew;
				lock (m_leasedSessions)
				{
					m_leasedSessions.Add(session.Id, session);
					leasedSessionsCountNew = m_leasedSessions.Count;
				}
				if (Log.IsDebugEnabled())
					Log.Debug("{0} returning new Session{1} to caller; m_leasedSessions.Count={2}", m_logArguments[0], session.Id, leasedSessionsCountNew);
				return session;
			}
			catch
			{
				m_sessionSemaphore.Release();
				throw;
			}
		}

		private bool SessionIsHealthy(ServerSession session)
		{
			if (!session.IsConnected)
				return false;
			if (session.PoolGeneration != m_generation)
				return false;
			if (session.DatabaseOverride != null)
				return false;
			if (m_connectionSettings.ConnectionLifeTime > 0
			    && (DateTime.UtcNow - session.CreatedUtc).TotalSeconds >= m_connectionSettings.ConnectionLifeTime)
				return false;

			return true;
		}

		public void Return(ServerSession session)
		{
			if (Log.IsDebugEnabled())
				Log.Debug("{0} receiving Session{1} back", m_logArguments[0], session.Id);

			try
			{
				lock (m_leasedSessions)
					m_leasedSessions.Remove(session.Id);
				session.OwningConnection = null;
				if (SessionIsHealthy(session))
				{
					lock (m_sessions)
						m_sessions.AddFirst(session);
				}
				else
				{
					Log.Warn("{0} received invalid Session{1}; destroying it", m_logArguments[0], session.Id);
					AdjustHostConnectionCount(session, -1);
					session.DisposeAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
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
			Log.Info("{0} clearing connection pool", m_logArguments);
			Interlocked.Increment(ref m_generation);
			RecoverLeakedSessions();
			await CleanPoolAsync(ioBehavior, session => session.PoolGeneration != m_generation, false, cancellationToken).ConfigureAwait(false);
		}

		public async Task ReapAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			Log.Debug("{0} reaping connection pool", m_logArguments);
			RecoverLeakedSessions();
			if (m_connectionSettings.ConnectionIdleTimeout == 0)
				return;
			await CleanPoolAsync(ioBehavior, session => (DateTime.UtcNow - session.LastReturnedUtc).TotalSeconds >= m_connectionSettings.ConnectionIdleTimeout, true, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Examines all the <see cref="ServerSession"/> objects in <see cref="m_leasedSessions"/> to determine if any
		/// have an owning <see cref="MySqlConnection"/> that has been garbage-collected. If so, assumes that the connection
		/// was not properly disposed and returns the session to the pool.
		/// </summary>
		private void RecoverLeakedSessions()
		{
			var recoveredSessions = new List<ServerSession>();
			lock (m_leasedSessions)
			{
				m_lastRecoveryTime = unchecked((uint) Environment.TickCount);
				foreach (var session in m_leasedSessions.Values)
				{
					if (!session.OwningConnection.TryGetTarget(out var _))
						recoveredSessions.Add(session);
				}
			}
			if (recoveredSessions.Count == 0)
				Log.Debug("{0} recovered no sessions", m_logArguments);
			else
				Log.Warn("{0} recovered {1} sessions", m_logArguments[0], recoveredSessions.Count);
			foreach (var session in recoveredSessions)
				session.ReturnToPool();
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
						lock (m_sessions)
							if (m_connectionSettings.MaximumPoolSize - m_sessionSemaphore.CurrentCount + m_sessions.Count <= m_connectionSettings.MinimumPoolSize)
								return;

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
						ServerSession session = null;
						lock (m_sessions)
						{
							if (m_sessions.Count > 0)
							{
								session = m_sessions.Last.Value;
								m_sessions.RemoveLast();
							}
						}
						if (session == null)
							return;

						if (shouldCleanFn(session))
						{
							// session should be cleaned; dispose it and keep iterating
							Log.Info("{0} found Session{1} to clean up", m_logArguments[0], session.Id);
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

		private async Task CreateMinimumPooledSessions(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			while (true)
			{
				lock (m_sessions)
				{
					// check if the desired minimum number of sessions have been created
					if (m_connectionSettings.MaximumPoolSize - m_sessionSemaphore.CurrentCount + m_sessions.Count >= m_connectionSettings.MinimumPoolSize)
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
					var session = new ServerSession(this, m_generation, Interlocked.Increment(ref m_lastSessionId));
					Log.Info("{0} created Session{1} to reach minimum pool size", m_logArguments[0], session.Id);
					await session.ConnectAsync(m_connectionSettings, m_loadBalancer, ioBehavior, cancellationToken).ConfigureAwait(false);
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

		public static ConnectionPool GetPool(ConnectionSettings cs)
		{
			if (!cs.Pooling)
				return null;

			var key = cs.ConnectionString;

			try
			{
				s_poolLock.EnterReadLock();
				if (s_pools.TryGetValue(key, out var pool))
					return pool;
			}
			finally
			{
				s_poolLock.ExitReadLock();
			}

			try
			{
				s_poolLock.EnterWriteLock();
				if (!s_pools.TryGetValue(key, out var pool))
					pool = s_pools[key] = new ConnectionPool(cs);
				return pool;
			}
			finally
			{
				s_poolLock.ExitWriteLock();
			}
		}

		public static async Task ClearPoolsAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			foreach (var pool in GetAllPools())
				await pool.ClearAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
		}

		public static async Task ReapPoolsAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			foreach (var pool in GetAllPools())
				await pool.ReapAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
		}

		private static IReadOnlyList<ConnectionPool> GetAllPools()
		{
			try
			{
				s_poolLock.EnterReadLock();
				return s_pools.Values.ToList();
			}
			finally
			{
				s_poolLock.ExitReadLock();
			}
		}

		private ConnectionPool(ConnectionSettings cs)
		{
			m_connectionSettings = cs;
			m_generation = 0;
			m_cleanSemaphore = new SemaphoreSlim(1);
			m_sessionSemaphore = new SemaphoreSlim(cs.MaximumPoolSize);
			m_sessions = new LinkedList<ServerSession>();
			m_leasedSessions = new Dictionary<string, ServerSession>();
			if (cs.LoadBalance == MySqlLoadBalance.LeastConnections)
			{
				m_hostSessions = new Dictionary<string, int>();
				foreach (var hostName in cs.HostNames)
					m_hostSessions[hostName] = 0;
			}
			m_loadBalancer = cs.ConnectionType != ConnectionType.Tcp ? null :
				cs.HostNames.Count == 1 || cs.LoadBalance == MySqlLoadBalance.FailOver ? FailOverLoadBalancer.Instance :
				cs.LoadBalance == MySqlLoadBalance.Random ? RandomLoadBalancer.Instance :
				cs.LoadBalance == MySqlLoadBalance.LeastConnections ? new LeastConnectionsLoadBalancer(this) :
				(ILoadBalancer) new RoundRobinLoadBalancer();

			Id = Interlocked.Increment(ref s_poolId);
			m_logArguments = new object[] { "Pool{0}".FormatInvariant(Id) };
			if (Log.IsInfoEnabled())
			{
				var csb = new MySqlConnectionStringBuilder(cs.ConnectionString);
				Log.Info("{0} creating new connection pool for {1}", m_logArguments[0], csb.GetConnectionString(includePassword: false));
			}
		}

		private void AdjustHostConnectionCount(ServerSession session, int delta)
		{
			if (m_hostSessions != null)
			{
				lock (m_hostSessions)
					m_hostSessions[session.HostName] += delta;
			}
		}

		private sealed class LeastConnectionsLoadBalancer : ILoadBalancer
		{
			public LeastConnectionsLoadBalancer(ConnectionPool pool) => m_pool = pool;

			public IEnumerable<string> LoadBalance(IReadOnlyList<string> hosts)
			{
				lock (m_pool.m_hostSessions)
					return m_pool.m_hostSessions.OrderBy(x => x.Value).Select(x => x.Key).ToList();
			}

			readonly ConnectionPool m_pool;
		}

		static readonly IMySqlConnectorLogger Log = MySqlConnectorLogManager.CreateLogger(nameof(ConnectionPool));
		static readonly ReaderWriterLockSlim s_poolLock = new ReaderWriterLockSlim();
		static readonly Dictionary<string, ConnectionPool> s_pools = new Dictionary<string, ConnectionPool>();
#if DEBUG
		static readonly TimeSpan ReaperInterval = TimeSpan.FromSeconds(1);
#else
		static readonly TimeSpan ReaperInterval = TimeSpan.FromMinutes(1);
#endif
		static readonly Task Reaper = Task.Run(async () => {
			while (true)
			{
				var task = Task.Delay(ReaperInterval);
				try
				{
					await ReapPoolsAsync(IOBehavior.Asynchronous, new CancellationTokenSource(ReaperInterval).Token).ConfigureAwait(false);
				}
				catch
				{
					// do nothing; we'll try to reap again
				}
				await task.ConfigureAwait(false);
			}
		});

		static int s_poolId;

		int m_generation;
		readonly SemaphoreSlim m_cleanSemaphore;
		readonly SemaphoreSlim m_sessionSemaphore;
		readonly LinkedList<ServerSession> m_sessions;
		readonly ConnectionSettings m_connectionSettings;
		readonly Dictionary<string, ServerSession> m_leasedSessions;
		readonly ILoadBalancer m_loadBalancer;
		readonly Dictionary<string, int> m_hostSessions;
		readonly object[] m_logArguments;
		uint m_lastRecoveryTime;
		int m_lastSessionId;
	}
}
