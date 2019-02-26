using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Authentication;
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
		// General
		//
		// * When we're at capacity (Busy==Max) further open attempts wait until someone releases.
		//   This must happen in FIFO (first to block on open is the first to release), otherwise some attempts may get
		//   starved and time out. This is why we use a ConcurrentQueue.
		// * We must avoid a race condition whereby an open attempt starts waiting at the same time as another release
		//   puts a connector back into the idle list. This would potentially make the waiter wait forever/time out.
		//
		// Rules
		// * You *only* create a new connector if Total < Max.
		// * You *only* go into waiting if Busy == Max (which also implies Idle == 0)
		//
		// Implementation taken from https://github.com/npgsql/npgsql/blob/6f5e936ba3cff2c71e914528a282fa0d7f683c78/src/Npgsql/ConnectorPool.cs
		// This implementation should be kept up-to-date with changes in that code.

		public int Id { get; }

		public ConnectionSettings ConnectionSettings { get; }

		public SslProtocols SslProtocols { get; set; }

		public async ValueTask<ServerSession> GetSessionAsync(MySqlConnection connection, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			// if all sessions are used, see if any have been leaked and can be recovered
			// check at most once per second (although this isn't enforced via a mutex so multiple threads might block
			// on the lock in RecoverLeakedSessions in high-concurrency situations
			if (m_state.Busy == m_maximumPoolSize && unchecked(((uint) Environment.TickCount) - m_lastRecoveryTime) >= 1000u)
			{
				Log.Debug("Pool{0} is empty; checking for leaked sessions", m_logArguments);
				RecoverLeakedSessions();
			}

			Log.Debug("Pool{0} checking for an available session", m_logArguments);
			if (!TryAllocateFast(out var session))
			{
				// create a new session (if the pool isn't full), or wait for one
				session = await AllocateLong(connection, ioBehavior, cancellationToken).ConfigureAwait(false);
			}

			try
			{
				// if LastReturnedTicks==0, the session is newly created
				if (session.LastReturnedTicks != 0)
				{
					Log.Debug("Pool{0} found an existing session; checking it for validity", m_logArguments);
					bool reuseSession;

					if (session.PoolGeneration != m_generation)
					{
						Log.Debug("Pool{0} discarding session due to wrong generation", m_logArguments);
						reuseSession = false;
					}
					else
					{
						if (ConnectionSettings.ConnectionReset || session.DatabaseOverride != null)
						{
							reuseSession = await session.TryResetConnectionAsync(ConnectionSettings, ioBehavior, cancellationToken).ConfigureAwait(false);
						}
						else if ((unchecked((uint) Environment.TickCount) - session.LastReturnedTicks) >= ConnectionSettings.ConnectionIdlePingTime)
						{
							reuseSession = await session.TryPingAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
						}
						else
						{
							reuseSession = true;
						}
					}

					if (!reuseSession)
					{
						// session is either old or cannot communicate with the server
						Log.Warn("Pool{0} Session{1} is unusable; destroying it", m_logArguments[0], session.Id);
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
							m_leasedSessions.Add(session);
							leasedSessionsCountPooled = m_leasedSessions.Count;
						}
						if (Log.IsDebugEnabled())
							Log.Debug("Pool{0} returning pooled Session{1} to caller; LeasedSessionsCount={2}", m_logArguments[0], session.Id, leasedSessionsCountPooled);
						return session;
					}

					// create a new session
					session = new ServerSession(this, m_generation, Interlocked.Increment(ref m_lastSessionId));
					if (Log.IsInfoEnabled())
						Log.Info("Pool{0} no pooled session available; created new Session{1}", m_logArguments[0], session.Id);
					await session.ConnectAsync(ConnectionSettings, m_loadBalancer, ioBehavior, cancellationToken).ConfigureAwait(false);
				}

				AdjustHostConnectionCount(session, 1);
				session.OwningConnection = new WeakReference<MySqlConnection>(connection);
				int leasedSessionsCountNew;
				lock (m_leasedSessions)
				{
					m_leasedSessions.Add(session);
					leasedSessionsCountNew = m_leasedSessions.Count;
				}
				if (Log.IsDebugEnabled())
					Log.Debug("Pool{0} returning new Session{1} to caller; LeasedSessionsCount={2}", m_logArguments[0], session.Id, leasedSessionsCountNew);
				return session;
			}
			catch (Exception ex)
			{
				HandleExceptionCreatingSession(ex, session);
				throw;
			}
		}

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

		public void Return(ServerSession session)
		{
			if (Log.IsDebugEnabled())
				Log.Debug("Pool{0} receiving Session{1} back", m_logArguments[0], session.Id);

			lock (m_leasedSessions)
				m_leasedSessions.Remove(session);
			session.OwningConnection = null;
			var sessionHealth = GetSessionHealth(session);
			if (sessionHealth != 0)
			{
				if (sessionHealth == 1)
					Log.Warn("Pool{0} received invalid Session{1}; destroying it", m_logArguments[0], session.Id);
				else
					Log.Info("Pool{0} received expired Session{1}; destroying it", m_logArguments[0], session.Id);
				AdjustHostConnectionCount(session, -1);
				CloseSession(session, wasIdle: false);
				return;
			}

			var sw = new SpinWait();

			while (true)
			{
				var state = m_state.Copy();

				// If there are any pending open attempts in progress hand the session off to them directly.
				// Note that in this case, state changes (i.e. decrementing m_state.Waiting) happens at the allocating
				// side.
				if (state.Waiting > 0)
				{
					if (!m_waiting.TryDequeue(out var waitingOpenAttempt))
					{
						// _waitingCount has been increased, but there's nothing in the queue yet - someone is in the
						// process of enqueuing an open attempt. Wait and retry.
						sw.SpinOnce();
						continue;
					}

					var tcs = waitingOpenAttempt.TaskCompletionSource;

					// We have a pending open attempt. "Complete" it, handing off the session.
#if !NET45
					if (!tcs.TrySetResult(session))
					{
						// If the open attempt timed out, the Task's state will be set to Canceled and our TrySetResult fails. Try again.
						Debug.Assert(tcs.Task.IsCanceled);
						continue;
					}
#else
					if (waitingOpenAttempt.IsAsync)
					{
						// If the waiting open attempt is asynchronous (i.e. OpenAsync()), we can't simply
						// call SetResult on its TaskCompletionSource, since it would execute the open's
						// continuation in our thread (the closing thread). Instead we schedule the completion
						// to run in the thread pool via Task.Run().

						// We copy tcs2 and especially connector2 to avoid allocations caused by the closure, see
						// http://stackoverflow.com/questions/41507166/closure-heap-allocation-happening-at-start-of-method
						var tcs2 = tcs;
						var connector2 = session;

						// TODO: When we drop support for .NET Framework 4.5, switch to RunContinuationsAsynchronously
						Task.Run(() =>
						{
							if (!tcs2.TrySetResult(connector2))
							{
								// If the open attempt timed out, the Task's state will be set to Canceled and our
								// TrySetResult fails.
								// "Recursively" call Release() again, this will dequeue another open attempt and retry.
								Debug.Assert(tcs2.Task.IsCanceled);
								Return(connector2);
							}
						});
					}
					else if (!tcs.TrySetResult(session))  // Open attempt is sync
					{
						// If the open attempt timed out, the Task's state will be set to Canceled and our
						// TrySetResult fails. Try again.
						Debug.Assert(tcs.Task.IsCanceled);
						continue;
					}
#endif

					return;
				}

				// There were no waiting attempts. However, there's a race condition where a new waiting attempt
				// may occur as we're putting our session into the idle list. Decrement the busy
				// count, while atomically make sure the waiting count isn't increased.
				// Note that we also must update the state *before* putting the session back in the idle list.
				var newState = state;
				newState.Idle++;
				newState.Busy--;
				CheckInvariants(newState);
				if (Interlocked.CompareExchange(ref m_state.All, newState.All, state.All) != state.All)
				{
					// Our attempt to decrement the busy count failed, either because a waiting attempt has been added
					// or busy has changed. Loop again and retry.
					continue;
				}

				// If we're here, we successfully applied the new state above and can put the session back in the idle
				// list (there were no pending open attempts).

				var ticks = unchecked((uint) Environment.TickCount);
				if (ticks == 0)
					ticks = 1;
				session.LastReturnedTicks = ticks;

				sw = new SpinWait();
				while (true)
				{
					// place returned sessions starting at the end of the array so the pool has stack-like behaviour (i.e.,
					// the most-recently-returned session is more likely to be returned by the next call to TryAllocateFast)
					for (var i = m_idleSessions.Length - 1; i >= 0; i--)
					{
						if (m_idleSessions[i] is null && Interlocked.CompareExchange(ref m_idleSessions[i], session, null) is null)
							return;
					}

					sw.SpinOnce();
				}
			}
		}

		public Task ClearAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			Log.Info("Pool{0} clearing connection pool", m_logArguments);
			m_procedureCache = null;
			RecoverLeakedSessions();
			Clear();
			return Utility.CompletedTask;
		}

		/// <summary>
		/// Returns the stored procedure cache for this <see cref="ConnectionPool"/>, lazily creating it on demand.
		/// This method may return a different object after <see cref="ClearAsync"/> has been called. The returned
		/// object is shared between multiple threads and is only safe to use after taking a <c>lock</c> on the
		/// object itself.
		/// </summary>
		public Dictionary<string, CachedProcedure> GetProcedureCache()
		{
			var procedureCache = m_procedureCache;
			if (procedureCache == null)
			{
				var newProcedureCache = new Dictionary<string, CachedProcedure>();
				procedureCache = Interlocked.CompareExchange(ref m_procedureCache, newProcedureCache, null) ?? newProcedureCache;
			}
			return procedureCache;
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
				foreach (var session in m_leasedSessions)
				{
					if (!session.OwningConnection.TryGetTarget(out var _))
						recoveredSessions.Add(session);
				}
			}
			if (recoveredSessions.Count == 0)
				Log.Debug("Pool{0} recovered no sessions", m_logArguments);
			else
				Log.Warn("Pool{0}: RecoveredSessionCount={1}", m_logArguments[0], recoveredSessions.Count);
			foreach (var session in recoveredSessions)
				session.ReturnToPool();
		}

		public static ConnectionPool GetPool(string connectionString)
		{
			// check single-entry MRU cache for this exact connection string; most applications have just one
			// connection string and will get a cache hit here
			var cache = s_mruCache;
			if (cache?.ConnectionString == connectionString)
				return cache.Pool;

			// check if pool has already been created for this exact connection string
			if (s_pools.TryGetValue(connectionString, out var pool))
			{
				s_mruCache = new ConnectionStringPool(connectionString, pool);
				return pool;
			}

			// parse connection string and check for 'Pooling' setting; return 'null' if pooling is disabled
			var connectionStringBuilder = new MySqlConnectionStringBuilder(connectionString);
			if (!connectionStringBuilder.Pooling)
			{
				s_pools.GetOrAdd(connectionString, default(ConnectionPool));
				s_mruCache = new ConnectionStringPool(connectionString, null);
				return null;
			}

			// check for pool using normalized form of connection string
			var normalizedConnectionString = connectionStringBuilder.ConnectionString;
			if (normalizedConnectionString != connectionString && s_pools.TryGetValue(normalizedConnectionString, out pool))
			{
				// try to set the pool for the connection string to the canonical pool; if someone else
				// beats us to it, just use the existing value
				pool = s_pools.GetOrAdd(connectionString, pool);
				s_mruCache = new ConnectionStringPool(connectionString, pool);
				return pool;
			}

			// create a new pool and attempt to insert it; if someone else beats us to it, just use their value
			var connectionSettings = new ConnectionSettings(connectionStringBuilder);
			var newPool = new ConnectionPool(connectionSettings);
			pool = s_pools.GetOrAdd(normalizedConnectionString, newPool);

			if (pool == newPool)
			{
				s_mruCache = new ConnectionStringPool(connectionString, pool);

				// if we won the race to create the new pool, also store it under the original connection string
				if (connectionString != normalizedConnectionString)
					s_pools.GetOrAdd(connectionString, pool);
			}
			else if (pool != newPool && Log.IsInfoEnabled())
			{
				Log.Info("Pool{0} was created but will not be used (due to race)", newPool.m_logArguments);
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
				if (pool != null && uniquePools.Add(pool))
					pools.Add(pool);
			}
			return pools;
		}

		private ConnectionPool(ConnectionSettings cs)
		{
			ConnectionSettings = cs;
			SslProtocols = Utility.GetDefaultSslProtocols();
			m_generation = 0;
			m_maximumPoolSize = cs.MaximumPoolSize;
			m_minimumPoolSize = cs.MinimumPoolSize;
			m_pruningInterval = TimeSpan.FromSeconds(Math.Max(1, Math.Min(60, ConnectionSettings.ConnectionIdleTimeout / 2)));
			m_idleSessions = new ServerSession[m_maximumPoolSize];
			m_waiting = new ConcurrentQueue<OpenAttempt>();
			m_leasedSessions = new List<ServerSession>();
			if (cs.LoadBalance == MySqlLoadBalance.LeastConnections)
			{
				m_hostSessions = new Dictionary<string, int>();
				foreach (var hostName in cs.HostNames)
					m_hostSessions[hostName] = 0;
			}

			m_loadBalancer = cs.ConnectionProtocol != MySqlConnectionProtocol.Sockets ? null :
				cs.HostNames.Count == 1 || cs.LoadBalance == MySqlLoadBalance.FailOver ? FailOverLoadBalancer.Instance :
				cs.LoadBalance == MySqlLoadBalance.Random ? RandomLoadBalancer.Instance :
				cs.LoadBalance == MySqlLoadBalance.LeastConnections ? new LeastConnectionsLoadBalancer(this) :
				(ILoadBalancer) new RoundRobinLoadBalancer();

			Id = Interlocked.Increment(ref s_poolId);
			m_logArguments = new object[] { "{0}".FormatInvariant(Id) };
			if (Log.IsInfoEnabled())
				Log.Info("Pool{0} creating new connection pool for ConnectionString: {1}", m_logArguments[0], cs.ConnectionStringBuilder.GetConnectionString(includePassword: false));
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

		private sealed class ConnectionStringPool
		{
			public ConnectionStringPool(string connectionString, ConnectionPool pool)
			{
				ConnectionString = connectionString;
				Pool = pool;
			}

			public string ConnectionString { get; }
			public ConnectionPool Pool { get; }
		}

		private readonly struct OpenAttempt
		{
			public OpenAttempt(TaskCompletionSource<ServerSession> taskCompletionSource, bool isAsync)
			{
				TaskCompletionSource = taskCompletionSource;
				IsAsync = isAsync;
			}

			public readonly TaskCompletionSource<ServerSession> TaskCompletionSource;
			public readonly bool IsAsync;
		}

		[StructLayout(LayoutKind.Explicit)]
		private struct PoolState
		{
			[FieldOffset(0)]
			public short Idle;
			[FieldOffset(2)]
			public short Busy;
			[FieldOffset(4)]
			public short Waiting;
			[FieldOffset(0)]
			public long All;

			public int Total => Idle + Busy;

			public PoolState Copy() => new PoolState { All = Volatile.Read(ref All) };

			public override string ToString()
			{
				var state = Copy();
				return $"[{state.Total} total, {state.Idle} idle, {state.Busy} busy, {state.Waiting} waiting]";
			}
		}

#if !NETSTANDARD1_3
		static ConnectionPool()
		{
			AppDomain.CurrentDomain.DomainUnload += OnAppDomainShutDown;
			AppDomain.CurrentDomain.ProcessExit += OnAppDomainShutDown;
		}

		static void OnAppDomainShutDown(object sender, EventArgs e) => ClearPoolsAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
#endif

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool TryAllocateFast(out ServerSession session)
		{
			// Idle may indicate that there are idle connectors, with the subsequent scan failing to find any.
			// This can happen because of race conditions with Release(), which updates Idle before actually putting
			// the session in the list, or because of other allocation attempts, which remove the session from
			// the idle list before updating Idle.
			// Loop until either m_state.Idle is 0 or you manage to remove a session.
			session = null;
			while (Volatile.Read(ref m_state.Idle) > 0)
			{
				// read from the front of the array to simulate stack-like behaviour (see comment in Return)
				for (var i = 0; i < m_idleSessions.Length; i++)
				{
					// First check without an Interlocked operation, it's faster
					// If we see a session in this slot, atomically exchange it with a null.
					// Either we get a session out which we can use, or we get null because
					// someone has taken it in the meanwhile. Either way put a null in its place.
					if (!(m_idleSessions[i] is null) && !((session = Interlocked.Exchange(ref m_idleSessions[i], null)) is null))
						break;
				}

				if (session is null)
					return false;

				// We successfully extracted an idle session, update state
				var sw = new SpinWait();
				while (true)
				{
					var state = m_state.Copy();
					var newState = state;
					newState.Busy++;
					newState.Idle--;
					CheckInvariants(newState);
					if (Interlocked.CompareExchange(ref m_state.All, newState.All, state.All) == state.All)
						return true;
					sw.SpinOnce();
				}
			}

			session = null;
			return false;
		}

		private async ValueTask<ServerSession> AllocateLong(MySqlConnection connection, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			// No idle session was found in the pool.
			// We now loop until one of three things happen:
			// 1. The pool isn't at max capacity (Total < Max), so we can create a new physical connection.
			// 2. The pool is at maximum capacity and there are no idle connectors (Busy == Max),
			// so we enqueue an open attempt into the waiting queue, so that the next release will unblock it.
			// 3. An session makes it into the idle list (race condition with another Release().
			while (true)
			{
				ServerSession session;
				var state = m_state.Copy();
				var newState = state;

				if (state.Total < m_maximumPoolSize)
				{
					// We're under the pool's max capacity, try to "allocate" a slot for a new physical connection.
					newState.Busy++;
					CheckInvariants(newState);
					if (Interlocked.CompareExchange(ref m_state.All, newState.All, state.All) != state.All)
					{
						// Our attempt to increment the busy count failed, Loop again and retry.
						continue;
					}

					// We've managed to increase the busy counter, open a physical connection
					session = new ServerSession(this, m_generation, Interlocked.Increment(ref m_lastSessionId));
					if (Log.IsInfoEnabled())
						Log.Info("Pool{0} no pooled session available; created new Session{1}", m_logArguments[0], session.Id);

					try
					{
						await session.ConnectAsync(ConnectionSettings, m_loadBalancer, ioBehavior, cancellationToken).ConfigureAwait(false);
					}
					catch (Exception ex)
					{
						HandleExceptionCreatingSession(ex, session);
						throw;
					}

					// Start the pruning timer if we're above MinPoolSize
					if (m_pruningTimer is null && newState.Total > m_minimumPoolSize)
					{
						Log.Debug("Pool{0} starting pruning timer", m_logArguments);
						var newPruningTimer = new Timer(PruneIdleConnectors, null, Timeout.Infinite, Timeout.Infinite);
						if (Interlocked.CompareExchange(ref m_pruningTimer, newPruningTimer, null) is null)
						{
							newPruningTimer.Change(m_pruningInterval, m_pruningInterval);
						}
						else
						{
							// Someone beat us to it
							newPruningTimer.Dispose();
						}
					}

					return session;
				}

				if (state.Busy == m_maximumPoolSize)
				{
					// Pool is exhausted. Increase the waiting count while atomically making sure the busy count
					// doesn't decrease (otherwise we have a new idle session).
					newState.Waiting++;
					CheckInvariants(newState);
					if (Interlocked.CompareExchange(ref m_state.All, newState.All, state.All) != state.All)
					{
						// Our attempt to increment the waiting count failed, either because a session became idle (busy
						// changed) or the waiting count changed. Loop again and retry.
						continue;
					}

					// At this point the waiting count is non-zero, so new release calls are blocking on the waiting
					// queue. This avoids a race condition where we wait while another session is put back in the
					// idle list - we know the idle list is empty and will stay empty.
					Log.Debug("Pool{0} waiting for an available session", m_logArguments);

					try
					{
						// Enqueue an open attempt into the waiting queue so that the next release attempt will unblock us.
#if !NET45
						var tcs = new TaskCompletionSource<ServerSession>(TaskCreationOptions.RunContinuationsAsynchronously);
#else
						var tcs = new TaskCompletionSource<ServerSession>();
#endif
						m_waiting.Enqueue(new OpenAttempt(tcs, ioBehavior == IOBehavior.Asynchronous));

						try
						{
							using (cancellationToken.Register(() => tcs.SetCanceled()))
							{
								if (ioBehavior == IOBehavior.Asynchronous)
									await tcs.Task.ConfigureAwait(false);
								else
									tcs.Task.GetAwaiter().GetResult();
							}
						}
						catch (Exception)
						{
							// We're here if the cancellation token was triggered (possibly due to timeout)
							// Transition our Task to cancelled, so that the next time someone releases
							// a connection they'll skip over it.
							tcs.TrySetCanceled();

							// There's still a chance of a race condition, whereby the task was transitioned to
							// completed in the meantime.
							if (tcs.Task.Status != TaskStatus.RanToCompletion)
								throw;
						}

						Debug.Assert(tcs.Task.IsCompleted);
						session = tcs.Task.Result;

						// Our task completion may contain a null in order to unblock us, allowing us to try
						// allocating again.
						if (session is null)
						{
							Log.Debug("Pool{0} waiting received null session; trying again", m_logArguments);
							continue;
						}

						return session;
					}
					finally
					{
						// The allocation attempt succeeded or timed out, decrement the waiting count
						var sw = new SpinWait();
						while (true)
						{
							state = m_state.Copy();
							newState = state;
							newState.Waiting--;
							CheckInvariants(newState);
							if (Interlocked.CompareExchange(ref m_state.All, newState.All, state.All) == state.All)
								break;
							sw.SpinOnce();
						}
					}
				}

				// We didn't create a new session or start waiting, which means there's a new idle session, try
				// getting it
				Debug.Assert(state.Idle > 0);
				if (TryAllocateFast(out session))
					return session;
			}

			// Cannot be here
		}

		private void HandleExceptionCreatingSession(Exception ex, ServerSession session)
		{
			if (session != null)
			{
				try
				{
					Log.Debug(ex, "Pool{0} disposing created Session{1} due to exception: {2}", m_logArguments[0], session.Id, ex.Message);
					AdjustHostConnectionCount(session, -1);
					session.DisposeAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
				}
				catch (Exception unexpectedException)
				{
					Log.Error(unexpectedException, "Pool{0} unexpected error in GetSessionAsync: {1}", m_logArguments[0], unexpectedException.Message);
				}
			}

			// physical open failed, decrement busy back down
			var sw = new SpinWait();
			while (true)
			{
				var state = m_state.Copy();
				var newState = state;
				newState.Busy--;
				if (Interlocked.CompareExchange(ref m_state.All, newState.All, state.All) != state.All)
				{
					// Our attempt to increment the busy count failed, Loop again and retry.
					sw.SpinOnce();
					continue;
				}

				break;
			}

			// There may be waiters because we raised the busy count (and failed). Release one waiter if there is one.
			if (m_waiting.TryDequeue(out var waitingOpenAttempt))
			{
				var tcs = waitingOpenAttempt.TaskCompletionSource;

#if !NET45
				if (!tcs.TrySetResult(null)) // Open attempt is sync
				{
					// TODO: Release more??
				}
#else
				// We have a pending open attempt. "Complete" it, handing off the session.
				if (waitingOpenAttempt.IsAsync)
				{
					// If the waiting open attempt is asynchronous (i.e. OpenAsync()), we can't simply
					// call SetResult on its TaskCompletionSource, since it would execute the open's
					// continuation in our thread (the closing thread). Instead we schedule the completion
					// to run in the thread pool via Task.Run().

					// TODO: When we drop support for .NET Framework 4.5, switch to RunContinuationsAsynchronously
					Task.Run(() =>
					{
						if (!tcs.TrySetResult(null))
						{
							// TODO: Release more??
						}
					});
				}
				else if (!tcs.TrySetResult(null)) // Open attempt is sync
				{
					// TODO: Release more??
				}
#endif
			}
		}

		private void CloseSession(ServerSession session, bool wasIdle)
		{
			try
			{
				session.DisposeAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();

				var sw = new SpinWait();
				while (true)
				{
					var state = m_state.Copy();
					var newState = state;
					if (wasIdle)
						newState.Idle--;
					else
						newState.Busy--;
					CheckInvariants(newState);
					if (Interlocked.CompareExchange(ref m_state.All, newState.All, state.All) == state.All)
						break;
					sw.SpinOnce();
				}
			}
			catch (Exception e)
			{
				Log.Warn("Exception while closing outdated session", e, session.Id);
			}

			while (m_pruningTimer != null && m_state.Total <= m_minimumPoolSize)
			{
				var oldTimer = m_pruningTimer;
				if (object.ReferenceEquals(Interlocked.CompareExchange(ref m_pruningTimer, null, oldTimer), oldTimer))
				{
					oldTimer.Dispose();
					break;
				}
			}
		}

		private void PruneIdleConnectors(object _)
		{
			var idleLifetime = ConnectionSettings.ConnectionIdleTimeout;

			// prune from the back of the array, which is likely to have the oldest sessions
			for (var i = m_idleSessions.Length -1; i >= 0; i--)
			{
				if (m_state.Total <= m_minimumPoolSize)
					return;

				var session = m_idleSessions[i];
				if (session is null || ((unchecked((uint) Environment.TickCount) - session.LastReturnedTicks) / 1000) < idleLifetime)
					continue;
				if (Interlocked.CompareExchange(ref m_idleSessions[i], null, session) == session)
					CloseSession(session, wasIdle: true);
			}
		}

		private void Clear()
		{
			for (var i = 0; i < m_idleSessions.Length; i++)
			{
				var connector = Interlocked.Exchange(ref m_idleSessions[i], null);
				if (connector != null)
					CloseSession(connector, wasIdle: true);
			}

			Interlocked.Increment(ref m_generation);
		}

		[Conditional("DEBUG")]
		private void CheckInvariants(PoolState state)
		{
			if (state.Total > m_maximumPoolSize)
				throw new InvalidOperationException($"Pool is over capacity (Total={state.Total}, Max={m_maximumPoolSize})");
			if (state.Waiting > 0 && state.Idle > 0)
				throw new InvalidOperationException($"Can't have waiters ({state.Waiting}) while there are idle connections ({state.Idle}");
			if (state.Idle < 0)
				throw new InvalidOperationException("Idle is negative");
			if (state.Busy < 0)
				throw new InvalidOperationException("Busy is negative");
			if (state.Waiting < 0)
				throw new InvalidOperationException("Waiting is negative");
		}

		static readonly IMySqlConnectorLogger Log = MySqlConnectorLogManager.CreateLogger(nameof(ConnectionPool));
		static readonly ConcurrentDictionary<string, ConnectionPool> s_pools = new ConcurrentDictionary<string, ConnectionPool>();

		static int s_poolId;
		static ConnectionStringPool s_mruCache;

		int m_generation;
		readonly int m_maximumPoolSize;
		readonly int m_minimumPoolSize;
		readonly ServerSession[] m_idleSessions;
		readonly ConcurrentQueue<OpenAttempt> m_waiting;
		readonly List<ServerSession> m_leasedSessions;
		readonly ILoadBalancer m_loadBalancer;
		readonly Dictionary<string, int> m_hostSessions;
		readonly object[] m_logArguments;
		readonly TimeSpan m_pruningInterval;
		Timer m_pruningTimer;
		uint m_lastRecoveryTime;
		int m_lastSessionId;
		Dictionary<string, CachedProcedure> m_procedureCache;
		PoolState m_state;

		/// <summary>
		/// Maximum number of possible connections in any single pool.
		/// </summary>
		internal const int PoolSizeLimit = 4096;
	}
}
