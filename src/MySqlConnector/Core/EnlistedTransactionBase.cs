#if !NETSTANDARD1_3
using System;
using System.Transactions;

namespace MySqlConnector.Core
{
	internal abstract class EnlistedTransactionBase : IEnlistmentNotification
	{
		// A MySqlConnection that holds the ServerSession that was enrolled in the transaction
		public MySqlConnection Connection { get; set; }

		// Whether the connection is idle, i.e., a client has closed it and is no longer using it
		public bool IsIdle { get; set; }

		public Transaction Transaction { get; private set; }

		public void Start()
		{
			OnStart();
			Transaction!.EnlistVolatile(this, EnlistmentOptions.None);
		}

		void IEnlistmentNotification.Prepare(PreparingEnlistment enlistment)
		{
			OnPrepare(enlistment);
			enlistment.Prepared();
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
}
#endif
