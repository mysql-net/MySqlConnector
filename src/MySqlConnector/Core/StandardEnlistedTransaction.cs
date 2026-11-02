using System.Transactions;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace MySqlConnector.Core;

internal sealed class StandardEnlistedTransaction(Transaction transaction, MySqlConnection connection)
	: EnlistedTransactionBase(transaction, connection)
{
	protected override void OnStart()
	{
		SetSessionIsolationLevel();

		var consistentSnapshotText = Transaction.IsolationLevel == IsolationLevel.Snapshot ? " with consistent snapshot" : "";
		using var cmd = new MySqlCommand($"start transaction{consistentSnapshotText};", Connection);
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
