using System.Collections.Generic;
using MySql.Data.Serialization;

namespace MySql.Data.MySqlClient
{
	internal sealed class ConnectionPool
	{
		public MySqlSession TryGetSession()
		{
			lock (m_lock)
			{
				if (m_sessions.Count > 0)
					return m_sessions.Dequeue();
			}
			return null;
		}

		public bool Return(MySqlSession session)
		{
			lock (m_lock)
			{
				// TODO: Dispose oldest connections in the pool first?
				if (m_sessions.Count >= m_maximumSize)
					return false;
				m_sessions.Enqueue(session);
				return true;
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

		public static void ClearPools()
		{
			List<ConnectionPool> pools;
			lock (s_lock)
				pools = new List<ConnectionPool>(s_pools.Values);

			foreach (var pool in pools)
				pool.Clear();
		}

		private ConnectionPool(int minimumSize, int maximumSize)
		{
			m_lock = new object();
			m_sessions = new Queue<MySqlSession>();
			m_minimumSize = minimumSize;
			m_maximumSize = maximumSize;
		}

		private void Clear()
		{
			lock (m_lock)
			{
				while (m_sessions.Count > 0)
				{
					var session = m_sessions.Dequeue();
					session.Dispose();
				}
			}
		}

		static readonly object s_lock = new object();
		static readonly Dictionary<string, ConnectionPool> s_pools = new Dictionary<string, ConnectionPool>();

		readonly object m_lock;
		readonly Queue<MySqlSession> m_sessions;
		readonly int m_minimumSize;
		readonly int m_maximumSize;
	}
}
