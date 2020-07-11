using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector
{
	public sealed class MySqlTransaction : DbTransaction
	{
		public override void Commit() => CommitAsync(IOBehavior.Synchronous, default).GetAwaiter().GetResult();
#if NET45 || NET461 || NET471 || NETSTANDARD1_3 || NETSTANDARD2_0 || NETCOREAPP2_1
		public Task CommitAsync(CancellationToken cancellationToken = default) => CommitAsync(Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, cancellationToken);
#else
		public override Task CommitAsync(CancellationToken cancellationToken = default) => CommitAsync(Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, cancellationToken);
#endif

		private async Task CommitAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			VerifyValid();

			using (var cmd = new MySqlCommand("commit", Connection, this))
				await cmd.ExecuteNonQueryAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			Connection!.CurrentTransaction = null;
			Connection = null;
		}

		public override void Rollback() => RollbackAsync(IOBehavior.Synchronous, default).GetAwaiter().GetResult();
#if NET45 || NET461 || NET471 || NETSTANDARD1_3 || NETSTANDARD2_0 || NETCOREAPP2_1
		public Task RollbackAsync(CancellationToken cancellationToken = default) => RollbackAsync(Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, cancellationToken);
#else
		public override Task RollbackAsync(CancellationToken cancellationToken = default) => RollbackAsync(Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, cancellationToken);
#endif

		private async Task RollbackAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			VerifyValid();

			using (var cmd = new MySqlCommand("rollback", Connection, this))
				await cmd.ExecuteNonQueryAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			Connection!.CurrentTransaction = null;
			Connection = null;
		}

		/// <summary>
		/// Removes the named transaction savepoint with the specified <paramref name="savepointName"/>. No commit or rollback occurs.
		/// </summary>
		/// <param name="savepointName">The savepoint name.</param>
		/// <remarks>The proposed ADO.NET API that this is based on is not finalized; this API may change in the future.</remarks>
		public void Release(string savepointName) => ExecuteSavepointAsync("release ", savepointName, IOBehavior.Synchronous, default).GetAwaiter().GetResult();

		/// <summary>
		/// Asynchronously removes the named transaction savepoint with the specified <paramref name="savepointName"/>. No commit or rollback occurs.
		/// </summary>
		/// <param name="savepointName">The savepoint name.</param>
		/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		/// <remarks>The proposed ADO.NET API that this is based on is not finalized; this API may change in the future.</remarks>
		public Task ReleaseAsync(string savepointName, CancellationToken cancellationToken = default) => ExecuteSavepointAsync("release ", savepointName, Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, cancellationToken);

		/// <summary>
		/// Rolls back the current transaction to the savepoint with the specified <paramref name="savepointName"/> without aborting the transaction.
		/// </summary>
		/// <param name="savepointName">The savepoint name.</param>
		/// <remarks><para>The name must have been created with <see cref="Save"/>, but not released by calling <see cref="Release"/>.</para>
		/// <para>The proposed ADO.NET API that this is based on is not finalized; this API may change in the future.</para></remarks>
		public void Rollback(string savepointName) => ExecuteSavepointAsync("rollback to ", savepointName, IOBehavior.Synchronous, default).GetAwaiter().GetResult();

		/// <summary>
		/// Asynchronously rolls back the current transaction to the savepoint with the specified <paramref name="savepointName"/> without aborting the transaction.
		/// </summary>
		/// <param name="savepointName">The savepoint name.</param>
		/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		/// <remarks><para>The name must have been created with <see cref="SaveAsync"/>, but not released by calling <see cref="ReleaseAsync"/>.</para>
		/// <para>The proposed ADO.NET API that this is based on is not finalized; this API may change in the future.</para></remarks>
		public Task RollbackAsync(string savepointName, CancellationToken cancellationToken = default) => ExecuteSavepointAsync("rollback to ", savepointName, Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, cancellationToken);

		/// <summary>
		/// Sets a named transaction savepoint with the specified <paramref name="savepointName"/>. If the current transaction
		/// already has a savepoint with the same name, the old savepoint is deleted and a new one is set.
		/// </summary>
		/// <param name="savepointName">The savepoint name.</param>
		/// <remarks>The proposed ADO.NET API that this is based on is not finalized; this API may change in the future.</remarks>
		public void Save(string savepointName) => ExecuteSavepointAsync("", savepointName, IOBehavior.Synchronous, default).GetAwaiter().GetResult();

		/// <summary>
		/// Asynchronously sets a named transaction savepoint with the specified <paramref name="savepointName"/>. If the current transaction
		/// already has a savepoint with the same name, the old savepoint is deleted and a new one is set.
		/// </summary>
		/// <param name="savepointName">The savepoint name.</param>
		/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		/// <remarks>The proposed ADO.NET API that this is based on is not finalized; this API may change in the future.</remarks>
		public Task SaveAsync(string savepointName, CancellationToken cancellationToken = default) => ExecuteSavepointAsync("", savepointName, Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, cancellationToken);

		private async Task ExecuteSavepointAsync(string command, string savepointName, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			VerifyValid();

			if (savepointName is null)
				throw new ArgumentNullException(nameof(savepointName));
			if (savepointName.Length == 0)
				throw new ArgumentException("savepointName must not be empty", nameof(savepointName));

			using var cmd = new MySqlCommand(command + "savepoint " + QuoteIdentifier(savepointName), Connection, this);
			await cmd.ExecuteNonQueryAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
		}

		public new MySqlConnection? Connection { get; private set; }
		protected override DbConnection? DbConnection => Connection;
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

#if NET45 || NET461 || NET471 || NETSTANDARD1_3 || NETSTANDARD2_0 || NETCOREAPP2_1
		public Task DisposeAsync() => DisposeAsync(Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, CancellationToken.None);
#else
		public override ValueTask DisposeAsync() => DisposeAsync(Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, CancellationToken.None);
#endif

#if NET45 || NET461 || NET471 || NETSTANDARD1_3 || NETSTANDARD2_0 || NETCOREAPP2_1
		internal Task DisposeAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
#else
		internal ValueTask DisposeAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
#endif
		{
			m_isDisposed = true;
			if (Connection?.CurrentTransaction == this)
				return DoDisposeAsync(ioBehavior, cancellationToken);
			Connection = null;
#if NET45 || NET461 || NET471 || NETSTANDARD1_3 || NETSTANDARD2_0 || NETCOREAPP2_1
			return Utility.CompletedTask;
#else
			return default;
#endif
		}

#if NET45 || NET461 || NET471 || NETSTANDARD1_3 || NETSTANDARD2_0 || NETCOREAPP2_1
		private async Task DoDisposeAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
#else
		private async ValueTask DoDisposeAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
#endif
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

		private void VerifyValid()
		{
			if (m_isDisposed)
				throw new ObjectDisposedException(nameof(MySqlTransaction));
			if (Connection is null)
				throw new InvalidOperationException("Already committed or rolled back.");
			if (Connection.CurrentTransaction is null)
				throw new InvalidOperationException("There is no active transaction.");
			if (!object.ReferenceEquals(Connection.CurrentTransaction, this))
				throw new InvalidOperationException("This is not the active transaction.");
		}

		private static string QuoteIdentifier(string identifier) => "`" + identifier.Replace("`", "``") + "`";

		bool m_isDisposed;
	}
}
