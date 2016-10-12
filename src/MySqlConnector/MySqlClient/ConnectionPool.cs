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
				await m_session_semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			else
				m_session_semaphore.Wait(cancellationToken);

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
						if (m_resetConnections)
						{
							await session.ResetConnectionAsync(m_userId, m_password, m_database, ioBehavior, cancellationToken).ConfigureAwait(false);
						}
						// pooled session is ready to be used; return it
						return session;
					}
				}

				session = new MySqlSession(this, m_generation);
				await session.ConnectAsync(m_servers, m_port, m_userId, m_password, m_database, m_sslMode, m_certificateFile, m_certificatePassword, ioBehavior, cancellationToken).ConfigureAwait(false);
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
				if (session.PoolGeneration == m_generation)
					m_sessions.Enqueue(session);
				else
					session.DisposeAsync(IOBehavior.Synchronous, CancellationToken.None).ConfigureAwait(false);
			}
			finally
			{
				m_session_semaphore.Release();
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
					if (!await m_session_semaphore.WaitAsync(waitTimeout, cancellationToken).ConfigureAwait(false))
						return;
				}
				else
				{
					if (!m_session_semaphore.Wait(waitTimeout, cancellationToken))
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
					m_session_semaphore.Release();
				}
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
				pool = s_pools.GetOrAdd(key, newKey => new ConnectionPool(csb.Server.Split(','), (int) csb.Port, csb.UserID, csb.Password, csb.Database,
					csb.SslMode, csb.CertificateFile, csb.CertificatePassword, csb.ConnectionReset, (int)csb.MinimumPoolSize, (int) csb.MaximumPoolSize));
			}
			return pool;
		}

		public static async Task ClearPoolsAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			var pools = new List<ConnectionPool>(s_pools.Values);

			foreach (var pool in pools)
				await pool.ClearAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
		}

		private ConnectionPool(IEnumerable<string> servers, int port, string userId, string password, string database, SslMode sslMode,
			string certificateFile, string certificatePassword, bool resetConnections, int minimumSize, int maximumSize)
		{
			m_servers = servers;
			m_port = port;
			m_userId = userId;
			m_password = password;
			m_database = database;
			m_resetConnections = resetConnections;
			m_sslMode = sslMode;
			m_certificateFile = certificateFile;
			m_certificatePassword = certificatePassword;
			m_minimumSize = minimumSize;
			m_maximumSize = maximumSize;

			m_generation = 0;
			m_session_semaphore = new SemaphoreSlim(m_maximumSize);
			m_sessions = new ConcurrentQueue<MySqlSession>();
		}

		static readonly ConcurrentDictionary<string, ConnectionPool> s_pools = new ConcurrentDictionary<string, ConnectionPool>();

		int m_generation;
		readonly SemaphoreSlim m_session_semaphore;
		readonly ConcurrentQueue<MySqlSession> m_sessions;

		readonly IEnumerable<string> m_servers;
		readonly int m_port;
		readonly string m_userId;
		readonly string m_password;
		readonly string m_database;
		readonly SslMode m_sslMode;
		readonly string m_certificateFile;
		readonly string m_certificatePassword;
		readonly bool m_resetConnections;
		readonly int m_minimumSize;
		readonly int m_maximumSize;
	}
}
