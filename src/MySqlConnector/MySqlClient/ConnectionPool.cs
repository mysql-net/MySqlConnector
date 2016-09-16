using System;
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
			AssertInvariants();

			using (cancellationToken.Register(() =>
			{
				// wake up all waiting threads if the cancellation token is cancelled
				lock (m_lock)
					Monitor.PulseAll(m_lock);
			}))
			{
				MySqlSession temporarySession = null;

				try
				{
					// keep looping until cancelled (timeout) or a session is retrieved
					while (true)
					{
						cancellationToken.ThrowIfCancellationRequested();

						bool isNew;
						AssertInvariants();

						// grab an idle session, or take an available session, or block
						lock (m_lock)
						{
							if (m_idleSessions.Count > 0)
							{
								temporarySession = m_idleSessions.Dequeue();
								isNew = false;
								m_temporary++;
							}
							else if (m_available > 0)
							{
								temporarySession = new MySqlSession(this);
								isNew = true;
								m_temporary++;
								m_available--;
							}
							else
							{
								Monitor.Wait(m_lock);
								continue;
							}
						}
						AssertInvariants();

						// asynchronously connect the session or verify that an idle session is valid
						bool isValid;
						if (isNew)
						{
							await temporarySession.ConnectAsync(m_server.Split(','), m_port, m_userId, m_password, m_database, cancellationToken).ConfigureAwait(false);
							isValid = true;
						}
						else
						{
							isValid = await TryActivateIdleSessionAsync(temporarySession, cancellationToken).ConfigureAwait(false);
						}

						// if valid, return it
						if (isValid)
						{
							lock (m_lock)
							{
								m_activeSessions.Add(temporarySession);
								m_temporary--;

								var session = temporarySession;
								temporarySession = null;
								return session;
							}
						}

						// destroy this invalid temporary session and try again
						lock (m_lock)
						{
							m_temporary--;
							m_available++;
							temporarySession = null;

							Monitor.Pulse(m_lock);
						}
					}
				}
				catch
				{
					// clean up any local temporary session upon exception
					lock (m_lock)
					{
						if (temporarySession != null)
						{
							m_temporary--;
							m_available++;
							Monitor.Pulse(m_lock);
						}
					}
					throw;
				}
				finally
				{
					AssertInvariants();
				}
			}
		}

		private async Task<bool> TryActivateIdleSessionAsync(MySqlSession session, CancellationToken cancellationToken)
		{
			// test that session is still valid and (optionally) reset it
			if (!await session.TryPingAsync(cancellationToken).ConfigureAwait(false))
			{
				await session.DisposeAsync(cancellationToken).ConfigureAwait(false);
				return false;
			}

			if (m_resetConnections)
				await session.ResetConnectionAsync(m_userId, m_password, m_database, cancellationToken).ConfigureAwait(false);
			return true;
		}
		
		public void Return(MySqlSession session)
		{
			AssertInvariants();

			lock (m_lock)
			{
				if (!m_activeSessions.Remove(session))
					throw new InvalidOperationException("Returned session wasn't active.");
				m_temporary++;
			}

			AssertInvariants();
			
			if (m_closeOnReturn)
			{
				session.DisposeAsync(CancellationToken.None).GetAwaiter().GetResult();
				lock (m_lock)
				{
					m_temporary--;
					m_available++;
					Monitor.Pulse(m_lock);
				}
			}
			else
			{
				lock (m_lock)
				{
					m_temporary--;
					m_idleSessions.Enqueue(session);
					Monitor.Pulse(m_lock);
				}
			}
			
			AssertInvariants();
		}

		public async Task ClearAsync(CancellationToken cancellationToken)
		{
			AssertInvariants();

			while (!cancellationToken.IsCancellationRequested)
			{
				MySqlSession session = null;

				// get one idle session at a time and clean it up
				AssertInvariants();
				lock (m_lock)
				{
					if (m_idleSessions.Count > 0)
					{
						session = m_idleSessions.Dequeue();
						m_temporary++;
					}
				}
				AssertInvariants();

				if (session == null)
					break;

				try
				{
					await session.DisposeAsync(cancellationToken).ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
				}
				finally
				{
					AssertInvariants();
					lock (m_lock)
					{
						m_temporary--;
						m_available++;
						Monitor.Pulse(m_lock);
					}
					AssertInvariants();
				}
			}

			AssertInvariants();
		}

		public static ConnectionPool GetPool(MySqlConnectionStringBuilder csb)
		{
			if (!csb.Pooling)
				return new ConnectionPool(csb.Server, (int) csb.Port, csb.UserID, csb.Password, csb.Database, csb.ConnectionReset, true, 0, 1);

			string key = csb.ConnectionString;
			lock (s_lock)
			{
				ConnectionPool pool;
				if (!s_pools.TryGetValue(key, out pool))
				{
					pool = new ConnectionPool(csb.Server, (int) csb.Port, csb.UserID, csb.Password, csb.Database, csb.ConnectionReset, false, (int) csb.MinimumPoolSize, (int) csb.MaximumPoolSize);
					s_pools.Add(key, pool);
				}
				return pool;
			}
		}

		public static async Task ClearPoolsAsync(CancellationToken cancellationToken)
		{
			List<ConnectionPool> pools;
			lock (s_lock)
				pools = new List<ConnectionPool>(s_pools.Values);

			foreach (var pool in pools)
				await pool.ClearAsync(cancellationToken).ConfigureAwait(false);
		}

		private ConnectionPool(string server, int port, string userId, string password, string database, bool resetConnections, bool closeOnReturn, int minimumSize, int maximumSize)
		{
			m_server = server;
			m_port = port;
			m_userId = userId;
			m_password = password;
			m_database = database;
			m_resetConnections = resetConnections;
			m_closeOnReturn = closeOnReturn;
			m_minimumSize = minimumSize;
			m_maximumSize = maximumSize;

			m_lock = new object();

			m_idleSessions = new Queue<MySqlSession>();
			m_activeSessions = new List<MySqlSession>();
			m_available = m_maximumSize;
			AssertInvariants();
		}

		private void AssertInvariants()
		{
			lock (m_lock)
			{
				// This invariant must always hold outside of the lock: the following values must sum to `m_maximumSize`:
				//   - m_activeSessions.Count: the number of sessions that are held by clients
				//   - m_idleSessions.Count: the number of sessions that have been returned to the pool and are idle
				//   - m_available: the number of sessions available to be created
				//   - m_temporary: the number of sessions actively in use by code in this class; these will soon be added to one of the other categories
				if (m_activeSessions.Count + m_idleSessions.Count + m_available + m_temporary != m_maximumSize)
					throw new InvalidOperationException("Invalid: Active:{0} Idle:{1} Avail:{2} Temp:{3} Max:{4}".FormatInvariant(m_activeSessions.Count, m_idleSessions.Count, m_available, m_temporary, m_maximumSize));
			}
		}

		static readonly object s_lock = new object();
		static readonly Dictionary<string, ConnectionPool> s_pools = new Dictionary<string, ConnectionPool>();

		readonly string m_server;
		readonly int m_port;
		readonly string m_userId;
		readonly string m_password;
		readonly string m_database;
		readonly bool m_resetConnections;
		readonly bool m_closeOnReturn;
		readonly int m_minimumSize;
		readonly int m_maximumSize;
		readonly object m_lock;
		readonly List<MySqlSession> m_activeSessions;
		readonly Queue<MySqlSession> m_idleSessions;
		int m_available;
		int m_temporary;
	}
}
