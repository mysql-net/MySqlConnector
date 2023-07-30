using System.Transactions;

namespace MySqlConnector.Core;

internal abstract class EnlistedTransactionBase : IEnlistmentNotification
{
	// A MySqlConnection that holds the ServerSession that was enrolled in the transaction
	public MySqlConnection Connection { get; set; }

	// Whether the connection is idle, i.e., a client has closed it and is no longer using it
	public bool IsIdle { get; set; }

	// Whether to enter the PREPARED state, The rollback operation needs to determine if the Xa transaction has entered the PREPARED state.
	public bool IsPrepared { get; set; }

	public Transaction Transaction { get; private set; }

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
		OnCommit(enlistment);
		enlistment.Done();
		Connection.UnenlistTransaction();
	}

	void IEnlistmentNotification.Rollback(Enlistment enlistment)
	{
		OnRollback(enlistment);
		enlistment.Done();
		Connection.UnenlistTransaction();
	}

	public void InDoubt(Enlistment enlistment) => throw new NotImplementedException();

	protected EnlistedTransactionBase(Transaction transaction, MySqlConnection connection)
	{
		Transaction = transaction;
		Connection = connection;
	}

	protected abstract void OnStart();
	protected abstract void OnPrepare(PreparingEnlistment enlistment);
	protected abstract void OnCommit(Enlistment enlistment);
	protected abstract void OnRollback(Enlistment enlistment);
}
