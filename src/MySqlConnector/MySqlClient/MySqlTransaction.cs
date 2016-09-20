using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
	public class MySqlTransaction : DbTransaction
	{
		public override void Commit()
		{
			CommitAsync().GetAwaiter().GetResult();
		}

		public async Task CommitAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			VerifyNotDisposed();
			if (m_isFinished)
				throw new InvalidOperationException("Already committed or rolled back.");

			if (m_connection.CurrentTransaction == this)
			{
				using (var cmd = new MySqlCommand("commit", m_connection, this))
					await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
				m_connection.CurrentTransaction = null;
				m_isFinished = true;
			}
			else if (m_connection.CurrentTransaction != null)
			{
				throw new InvalidOperationException("This is not the active transaction.");
			}
			else if (m_connection.CurrentTransaction == null)
			{
				throw new InvalidOperationException("There is no active transaction.");
			}
		}

		public override void Rollback()
		{
			RollbackAsync().GetAwaiter().GetResult();
		}

		public async Task RollbackAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			VerifyNotDisposed();
			if (m_isFinished)
				throw new InvalidOperationException("Already committed or rolled back.");

			if (m_connection.CurrentTransaction == this)
			{
				using (var cmd = new MySqlCommand("rollback", m_connection, this))
					await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
				m_connection.CurrentTransaction = null;
				m_isFinished = true;
			}
			else if (m_connection.CurrentTransaction != null)
			{
				throw new InvalidOperationException("This is not the active transaction.");
			}
			else if (m_connection.CurrentTransaction == null)
			{
				throw new InvalidOperationException("There is no active transaction.");
			}
		}

		protected override DbConnection DbConnection => m_connection;
		public override IsolationLevel IsolationLevel { get; }

		protected override void Dispose(bool disposing)
		{
			try
			{
				if (disposing)
				{
					if (!m_isFinished && m_connection != null && m_connection.CurrentTransaction == this)
					{
						using (var cmd = new MySqlCommand("rollback", m_connection, this))
							cmd.ExecuteNonQuery();
						m_connection.CurrentTransaction = null;
					}
					m_connection = null;
				}
			}
			finally
			{
				base.Dispose(disposing);
			}
		}


		internal MySqlTransaction(MySqlConnection connection, IsolationLevel isolationLevel)
		{
			m_connection = connection;
			IsolationLevel = isolationLevel;
		}

		private void VerifyNotDisposed()
		{
			if (m_connection == null)
				throw new ObjectDisposedException(nameof(MySqlTransaction));
		}

		MySqlConnection m_connection;
		bool m_isFinished;
	}
}
