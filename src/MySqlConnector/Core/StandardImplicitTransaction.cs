#if !NETSTANDARD1_3
using System;
using System.Transactions;
using MySql.Data.MySqlClient;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core
{
	internal sealed class StandardImplicitTransaction : ImplicitTransactionBase
	{
		public StandardImplicitTransaction(Transaction transaction, MySqlConnection connection)
			: base(transaction, connection)
		{
		}

		protected override void OnStart()
		{
			string isolationLevel;
			switch (Transaction.IsolationLevel)
			{
			case IsolationLevel.Serializable:
				isolationLevel = "serializable";
				break;
			case IsolationLevel.ReadCommitted:
				isolationLevel = "read committed";
				break;
			case IsolationLevel.ReadUncommitted:
				isolationLevel = "read uncommitted";
				break;
			case IsolationLevel.Snapshot:
			case IsolationLevel.Chaos:
				throw new NotSupportedException("IsolationLevel.{0} is not supported.".FormatInvariant(Transaction.IsolationLevel));
			// "In terms of the SQL:1992 transaction isolation levels, the default InnoDB level is REPEATABLE READ." - http://dev.mysql.com/doc/refman/5.7/en/innodb-transaction-model.html
			case IsolationLevel.Unspecified:
			case IsolationLevel.RepeatableRead:
			default:
				isolationLevel = "repeatable read";
				break;
			}

			using (var cmd = new MySqlCommand("set transaction isolation level " + isolationLevel + "; start transaction;", Connection))
				cmd.ExecuteNonQuery();
		}

		protected override void OnPrepare(PreparingEnlistment enlistment)
		{
		}

		protected override void OnCommit(Enlistment enlistment)
		{
			using (var cmd = new MySqlCommand("commit;", Connection))
				cmd.ExecuteNonQuery();
		}

		protected override void OnRollback(Enlistment enlistment)
		{
			using (var cmd = new MySqlCommand("rollback;", Connection))
				cmd.ExecuteNonQuery();
		}
	}
}
#endif
