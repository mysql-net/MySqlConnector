using System.Transactions;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace MySqlConnector.Core;

internal abstract class EnlistedTransactionBase(Transaction transaction, MySqlConnection connection) : IEnlistmentNotification
{
	// A MySqlConnection that holds the ServerSession that was enrolled in the transaction
	public MySqlConnection Connection { get; set; } = connection;

	// Whether the connection is idle, i.e., a client has closed it and is no longer using it
	public bool IsIdle { get; set; }

	// Whether the distributed transaction was prepared successfully
	public bool IsPrepared { get; private set; }

	public Transaction Transaction { get; private set; } = transaction;

	public void Start()
	{
		OnStart();
		Transaction!.EnlistVolatile(this, EnlistmentOptions.None);
	}

	void IEnlistmentNotification.Prepare(PreparingEnlistment preparingEnlistment)
	{
		try
		{
			OnPrepare(preparingEnlistment);
			IsPrepared = true;
			preparingEnlistment.Prepared();
		}
		catch (Exception ex)
		{
			preparingEnlistment.ForceRollback(ex);
		}
	}

	void IEnlistmentNotification.Commit(Enlistment enlistment)
	{
		try
		{
			OnCommit(enlistment);
		}
		finally
		{
			enlistment.Done();
			Connection.UnenlistTransaction();
		}
	}

	void IEnlistmentNotification.Rollback(Enlistment enlistment)
	{
		try
		{
			OnRollback(enlistment);
		}
		finally
		{
			enlistment.Done();
			Connection.UnenlistTransaction();
		}
	}

	public void InDoubt(Enlistment enlistment) => throw new NotImplementedException();

	// Sets the isolation level for the next transaction started in this session to the isolation level of the System.Transactions.Transaction
	protected void SetSessionIsolationLevel()
	{
		var isolationLevel = Transaction.IsolationLevel switch
		{
			IsolationLevel.Serializable => "serializable",
			IsolationLevel.ReadCommitted => "read committed",
			IsolationLevel.ReadUncommitted => "read uncommitted",
			IsolationLevel.RepeatableRead => "repeatable read",
			IsolationLevel.Snapshot => "repeatable read",
			IsolationLevel.Chaos => throw new NotSupportedException($"IsolationLevel.{Transaction.IsolationLevel} is not supported."),

			// "In terms of the SQL:1992 transaction isolation levels, the default InnoDB level is REPEATABLE READ." - http://dev.mysql.com/doc/refman/5.7/en/innodb-transaction-model.html
			IsolationLevel.Unspecified => "repeatable read",
			_ => "repeatable read",
		};

		using var cmd = new MySqlCommand($"set session transaction isolation level {isolationLevel};", Connection);
		cmd.ExecuteNonQuery();
	}

	protected abstract void OnStart();
	protected abstract void OnPrepare(PreparingEnlistment enlistment);
	protected abstract void OnCommit(Enlistment enlistment);
	protected abstract void OnRollback(Enlistment enlistment);
}
