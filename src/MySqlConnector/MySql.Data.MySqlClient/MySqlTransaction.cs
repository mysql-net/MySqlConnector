using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlTransaction : DbTransaction
	{
		public override void Commit() => CommitAsync(IOBehavior.Synchronous, default).GetAwaiter().GetResult();
		public Task CommitAsync() => CommitAsync(Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, default);
		public Task CommitAsync(CancellationToken cancellationToken) => CommitAsync(Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, cancellationToken);

		private async Task CommitAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			VerifyNotDisposed();
			if (Connection is null)
				throw new InvalidOperationException("Already committed or rolled back.");

			if (Connection.CurrentTransaction == this)
			{
				using (var cmd = new MySqlCommand("commit", Connection, this))
					await cmd.ExecuteNonQueryAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
				Connection.CurrentTransaction = null;
				Connection = null;
			}
			else if (Connection.CurrentTransaction != null)
			{
				throw new InvalidOperationException("This is not the active transaction.");
			}
			else if (Connection.CurrentTransaction is null)
			{
				throw new InvalidOperationException("There is no active transaction.");
			}
		}

		public override void Rollback() => RollbackAsync(IOBehavior.Synchronous, default).GetAwaiter().GetResult();
		public Task RollbackAsync() => RollbackAsync(Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, default);
		public Task RollbackAsync(CancellationToken cancellationToken) => RollbackAsync(Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, cancellationToken);

		private async Task RollbackAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			VerifyNotDisposed();
			if (Connection is null)
				throw new InvalidOperationException("Already committed or rolled back.");

			if (Connection.CurrentTransaction == this)
			{
				using (var cmd = new MySqlCommand("rollback", Connection, this))
					await cmd.ExecuteNonQueryAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
				Connection.CurrentTransaction = null;
				Connection = null;
			}
			else if (Connection.CurrentTransaction != null)
			{
				throw new InvalidOperationException("This is not the active transaction.");
			}
			else if (Connection.CurrentTransaction is null)
			{
				throw new InvalidOperationException("There is no active transaction.");
			}
		}

		public new MySqlConnection Connection { get; private set; }
		protected override DbConnection DbConnection => Connection;
		public override IsolationLevel IsolationLevel { get; }

		protected override void Dispose(bool disposing)
		{
			try
			{
				if (disposing)
						DisposeAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
			}
			finally
			{
				base.Dispose(disposing);
			}
		}

		public Task DisposeAsync() => DisposeAsync(Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, CancellationToken.None);

		internal Task DisposeAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			m_isDisposed = true;
			if (Connection?.CurrentTransaction == this)
				return DoDisposeAsync(ioBehavior, cancellationToken);
			Connection = null;
			return Utility.CompletedTask;
		}

		private async Task DoDisposeAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			if (Connection?.CurrentTransaction == this)
			{
				if (Connection.State == ConnectionState.Open && Connection.Session.IsConnected)
				{
					using (var cmd = new MySqlCommand("rollback", Connection, this))
						await cmd.ExecuteNonQueryAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
				}
				Connection.CurrentTransaction = null;
			}
			Connection = null;
		}

		internal MySqlTransaction(MySqlConnection connection, IsolationLevel isolationLevel)
		{
			Connection = connection;
			IsolationLevel = isolationLevel;
		}

		private void VerifyNotDisposed()
		{
			if (m_isDisposed)
				throw new ObjectDisposedException(nameof(MySqlTransaction));
		}

		bool m_isDisposed;
	}
}
