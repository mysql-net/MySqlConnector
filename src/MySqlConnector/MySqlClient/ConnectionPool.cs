using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.Serialization;

namespace MySql.Data.MySqlClient
{
	internal sealed class ConnectionPool
	{
		public async Task<MySqlSession> TryGetSessionAsync(CancellationToken cancellationToken)
		{
			try
			{
				await m_semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

				if (m_sessions.Count > 0)
					return m_sessions.Dequeue();
			}
			finally
			{
				m_semaphore.Release();
			}
			return null;
		}

		public bool Return(MySqlSession session)
		{
			try
			{
				m_semaphore.Wait();

				// TODO: Dispose oldest connections in the pool first?
				if (m_sessions.Count >= m_maximumSize)
					return false;
				m_sessions.Enqueue(session);
				return true;
			}
			finally
			{
				m_semaphore.Release();
			}
		}

		public async Task ClearAsync(CancellationToken cancellationToken)
		{
			try
			{
				await m_semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

				while (m_sessions.Count > 0)
				{
					cancellationToken.ThrowIfCancellationRequested();

					var session = m_sessions.Dequeue();
					await session.DisposeAsync(cancellationToken).ConfigureAwait(false);
				}
			}
			finally
			{
				m_semaphore.Release();
			}
		}

		public static ConnectionPool GetPool(MySqlConnectionStringBuilder csb)
		{
			if (!csb.Pooling)
				return null;

			string key = csb.ConnectionString;
			lock (s_lock)
			{
				ConnectionPool pool;
				if (!s_pools.TryGetValue(key, out pool))
				{
					pool = new ConnectionPool((int) csb.MinimumPoolSize, (int) csb.MaximumPoolSize);
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

		private ConnectionPool(int minimumSize, int maximumSize)
		{
			m_semaphore = new SemaphoreSlim(1, 1);
			m_sessions = new Queue<MySqlSession>();
			m_minimumSize = minimumSize;
			m_maximumSize = maximumSize;
		}

		static readonly object s_lock = new object();
		static readonly Dictionary<string, ConnectionPool> s_pools = new Dictionary<string, ConnectionPool>();

		readonly SemaphoreSlim m_semaphore;
		readonly Queue<MySqlSession> m_sessions;
		readonly int m_minimumSize;
		readonly int m_maximumSize;
	}
}
