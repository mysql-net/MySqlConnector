using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.Serialization;

namespace MySql.Data.MySqlClient
{
	internal sealed class ConnectionPool
	{
		public async Task<MySqlSession> GetSessionAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			// wait for an open slot
			if (ioBehavior == IOBehavior.Asynchronous)
				await m_sessionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			else
				m_sessionSemaphore.Wait(cancellationToken);

			try
			{
				MySqlSession session;
				// check for a pooled session
				if (m_sessions.TryDequeue(out session))
				{
					if (session.PoolGeneration != m_generation || !await session.TryPingAsync(ioBehavior, cancellationToken).ConfigureAwait(false))
					{
						// session is either old or cannot communicate with the server
						await session.DisposeAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
					}
					else
					{
						// session is valid, reset if supported
						if (m_connectionSettings.ConnectionReset)
						{
							await session.ResetConnectionAsync(m_connectionSettings, ioBehavior, cancellationToken).ConfigureAwait(false);
						}
						// pooled session is ready to be used; return it
						return session;
					}
				}

				session = new MySqlSession(this, m_generation);
				await session.ConnectAsync(m_connectionSettings, ioBehavior, cancellationToken).ConfigureAwait(false);
				return session;
			}
			catch
			{
				m_sessionSemaphore.Release();
				throw;
			}
		}

		public void Return(MySqlSession session)
		{
			try
			{
				if (session.PoolGeneration == m_generation)
					m_sessions.Enqueue(session);
				else
					session.DisposeAsync(IOBehavior.Synchronous, CancellationToken.None).ConfigureAwait(false);
			}
			finally
			{
				m_sessionSemaphore.Release();
			}
		}

		public async Task ClearAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			// increment the generation of the connection pool
			Interlocked.Increment(ref m_generation);

			var waitTimeout = TimeSpan.FromMilliseconds(10);
			while (true)
			{
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
					MySqlSession session;
					if (m_sessions.TryDequeue(out session))
					{
						if (session.PoolGeneration != m_generation)
						{
							// session generation does not match pool generation; dispose of it and continue iterating
							await session.DisposeAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
							continue;
						}
						else
						{
							// session generation matches pool generation; put it back in the queue and stop iterating
							m_sessions.Enqueue(session);
						}
					}
					return;
				}
				finally
				{
					m_sessionSemaphore.Release();
				}
			}
		}

		public static ConnectionPool GetPool(ConnectionSettings cs)
		{
			if (!cs.Pooling)
				return null;

			var key = cs.ConnectionString;

			ConnectionPool pool;
			if (!s_pools.TryGetValue(key, out pool))
			{
				pool = s_pools.GetOrAdd(cs.ConnectionString, newKey => new ConnectionPool(cs));
			}
			return pool;
		}

		public static async Task ClearPoolsAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			var pools = new List<ConnectionPool>(s_pools.Values);

			foreach (var pool in pools)
				await pool.ClearAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
		}

		private ConnectionPool(ConnectionSettings cs)
		{
			m_connectionSettings = cs;
			m_generation = 0;
			m_sessionSemaphore = new SemaphoreSlim(cs.MaximumPoolSize);
			m_sessions = new ConcurrentQueue<MySqlSession>();
		}

		static readonly ConcurrentDictionary<string, ConnectionPool> s_pools = new ConcurrentDictionary<string, ConnectionPool>();

		int m_generation;
		readonly SemaphoreSlim m_sessionSemaphore;
		readonly ConcurrentQueue<MySqlSession> m_sessions;
		readonly ConnectionSettings m_connectionSettings;
	}
}
