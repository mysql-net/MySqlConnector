using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector.Protocol.Serialization;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlTransaction : DbTransaction
	{
		public override void Commit() =>
			CommitAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();

		public Task CommitAsync(CancellationToken cancellationToken = default) =>
			CommitAsync(m_connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, cancellationToken);

		internal async Task CommitAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			VerifyNotDisposed();
			if (m_connection == null)
				throw new InvalidOperationException("Already committed or rolled back.");

			if (m_connection.CurrentTransaction == this)
			{
				using (var cmd = new MySqlCommand("commit", m_connection, this))
					await cmd.ExecuteNonQueryAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
				m_connection.CurrentTransaction = null;
				m_connection = null;
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

		public override void Rollback() =>
			RollbackAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();

		public Task RollbackAsync(CancellationToken cancellationToken = default) =>
			RollbackAsync(m_connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, cancellationToken);

		internal async Task RollbackAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			VerifyNotDisposed();
			if (m_connection == null)
				throw new InvalidOperationException("Already committed or rolled back.");

			if (m_connection.CurrentTransaction == this)
			{
				using (var cmd = new MySqlCommand("rollback", m_connection, this))
					await cmd.ExecuteNonQueryAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
				m_connection.CurrentTransaction = null;
				m_connection = null;
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

		public new MySqlConnection Connection => m_connection;
		protected override DbConnection DbConnection => m_connection;
		public override IsolationLevel IsolationLevel { get; }

		protected override void Dispose(bool disposing)
		{
			try
			{
				if (disposing)
				{
					m_isDisposed = true;
					if (m_connection?.CurrentTransaction == this)
					{
						if (m_connection.Session.IsConnected)
						{
							using (var cmd = new MySqlCommand("rollback", m_connection, this))
								cmd.ExecuteNonQuery();
						}
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
			if (m_isDisposed)
				throw new ObjectDisposedException(nameof(MySqlTransaction));
		}

		MySqlConnection m_connection;
		bool m_isDisposed;
	}
}
