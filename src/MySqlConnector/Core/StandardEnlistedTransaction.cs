#if !NETSTANDARD1_3
using System;
using System.Transactions;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core
{
	internal sealed class StandardEnlistedTransaction : EnlistedTransactionBase
	{
		public StandardEnlistedTransaction(Transaction transaction, MySqlConnection connection)
			: base(transaction, connection)
		{
		}

		protected override void OnStart()
		{
			var isolationLevel = Transaction.IsolationLevel switch
			{
				IsolationLevel.Serializable => "serializable",
				IsolationLevel.ReadCommitted => "read committed",
				IsolationLevel.ReadUncommitted => "read uncommitted",
				IsolationLevel.RepeatableRead => "repeatable read",
				IsolationLevel.Snapshot => "repeatable read",
				IsolationLevel.Chaos => throw new NotSupportedException("IsolationLevel.{0} is not supported.".FormatInvariant(Transaction.IsolationLevel)),

				// "In terms of the SQL:1992 transaction isolation levels, the default InnoDB level is REPEATABLE READ." - http://dev.mysql.com/doc/refman/5.7/en/innodb-transaction-model.html
				IsolationLevel.Unspecified => "repeatable read",
				_ => "repeatable read",
			};

			using var cmd = new MySqlCommand($"set session transaction isolation level {isolationLevel};", Connection);
			cmd.ExecuteNonQuery();

			var consistentSnapshotText = Transaction.IsolationLevel == IsolationLevel.Snapshot ? " with consistent snapshot" : "";
			cmd.CommandText = $"start transaction{consistentSnapshotText};";
			cmd.ExecuteNonQuery();
		}

		protected override void OnPrepare(PreparingEnlistment enlistment)
		{
		}

		protected override void OnCommit(Enlistment enlistment)
		{
			using var cmd = new MySqlCommand("commit;", Connection);
			cmd.ExecuteNonQuery();
		}

		protected override void OnRollback(Enlistment enlistment)
		{
			using var cmd = new MySqlCommand("rollback;", Connection);
			cmd.ExecuteNonQuery();
		}
	}
}
#endif
