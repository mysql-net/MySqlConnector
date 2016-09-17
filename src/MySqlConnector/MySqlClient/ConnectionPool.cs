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

		public async Task<MySqlSession> GetSessionAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			// if the drain lock is held, connection draining is in progress
			await m_drain_lock.WaitAsync(cancellationToken).ConfigureAwait(false);
			m_drain_lock.Release();

			// wait for an open slot
			await m_session_semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			try
			{
				MySqlSession session;
				// check for a pooled session
				if (m_sessions.TryDequeue(out session))
				{
					if (!await session.TryPingAsync(cancellationToken).ConfigureAwait(false))
					{
						// session is not valid
						await session.DisposeAsync(cancellationToken).ConfigureAwait(false);
					}
					else
					{
						// session is valid, reset if supported
						if (m_resetConnections)
						{
							await session.ResetConnectionAsync(m_userId, m_password, m_database, cancellationToken).ConfigureAwait(false);
						}
						// pooled session is ready to be used; return it
						return session;
					}
				}

				session = new MySqlSession(this);
				await session.ConnectAsync(m_servers, m_port, m_userId, m_password, m_database, m_connectionTimeout, cancellationToken).ConfigureAwait(false);
				return session;
			}
			catch
			{
				m_session_semaphore.Release();
				throw;
			}
		}

		public void Return(MySqlSession session)
		{
			try
			{
				m_sessions.Enqueue(session);
			}
			finally
			{
				m_session_semaphore.Release();
			}
		}

		public async Task ClearAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			try
			{
				// don't let any new connections out of the pool
				await m_drain_lock.WaitAsync(cancellationToken).ConfigureAwait(false);

				// acquire all of the session slots
				for (var i = 0; i < m_maximumSize; i++)
				{
					await m_session_semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

					// clear all of the existing sessions
					var tasks = new List<Task>();
					MySqlSession session;
					while (m_sessions.TryDequeue(out session))
					{
						tasks.Add(session.DisposeAsync(cancellationToken));
					}
					if (tasks.Count > 0)
					{
						await Task.WhenAll(tasks).ConfigureAwait(false);
					}
				}
			}
			finally
			{
				// release all of the session slots
				m_session_semaphore.Release(m_maximumSize);

				// release the master lock
				m_drain_lock.Release();
			}
		}

		public static ConnectionPool GetPool(MySqlConnectionStringBuilder csb)
		{
			if (!csb.Pooling)
				return null;

			var key = csb.ConnectionString;

			ConnectionPool pool;
			if (!s_pools.TryGetValue(key, out pool))
			{
				pool = s_pools.GetOrAdd(key, new ConnectionPool(csb.Server.Split(','), (int) csb.Port, csb.UserID,
						csb.Password, csb.Database, (int) csb.ConnectionTimeout, csb.ConnectionReset, (int)csb.MinimumPoolSize, (int) csb.MaximumPoolSize));
			}
			return pool;
		}

		public static async Task ClearPoolsAsync(CancellationToken cancellationToken)
		{
			var pools = new List<ConnectionPool>(s_pools.Values);

			foreach (var pool in pools)
				await pool.ClearAsync(cancellationToken).ConfigureAwait(false);
		}

		private ConnectionPool(IEnumerable<string> servers, int port, string userId, string password, string database, int connectionTimeout,
				bool resetConnections, int minimumSize, int maximumSize)
		{
			m_servers = servers;
			m_port = port;
			m_userId = userId;
			m_password = password;
			m_database = database;
			m_connectionTimeout = connectionTimeout;
			m_resetConnections = resetConnections;
			m_minimumSize = minimumSize;
			m_maximumSize = maximumSize;

			m_drain_lock = new SemaphoreSlim(1);
			m_session_semaphore = new SemaphoreSlim(m_maximumSize);
			m_sessions = new ConcurrentQueue<MySqlSession>();
		}

		static readonly ConcurrentDictionary<string, ConnectionPool> s_pools = new ConcurrentDictionary<string, ConnectionPool>();

		readonly SemaphoreSlim m_drain_lock;
		readonly SemaphoreSlim m_session_semaphore;
		readonly ConcurrentQueue<MySqlSession> m_sessions;

		readonly IEnumerable<string> m_servers;
		readonly int m_port;
		readonly string m_userId;
		readonly string m_password;
		readonly string m_database;
		readonly int m_connectionTimeout;
		readonly bool m_resetConnections;
		readonly int m_minimumSize;
		readonly int m_maximumSize;
	}
}
