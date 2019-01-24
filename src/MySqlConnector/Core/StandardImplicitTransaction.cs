#if !NETSTANDARD1_3
using System.Transactions;
using MySql.Data.MySqlClient;

namespace MySqlConnector.Core
{
	internal sealed class StandardImplicitTransaction : ImplicitTransactionBase
	{
		public StandardImplicitTransaction(MySqlConnection connection) : base(connection)
		{
		}

		protected override void OnStart()
		{
			System.Data.IsolationLevel isolationLevel;
			switch (Transaction.IsolationLevel)
			{
			case IsolationLevel.Serializable:
				isolationLevel = System.Data.IsolationLevel.Serializable;
				break;
			case IsolationLevel.RepeatableRead:
				isolationLevel = System.Data.IsolationLevel.RepeatableRead;
				break;
			case IsolationLevel.ReadCommitted:
				isolationLevel = System.Data.IsolationLevel.ReadCommitted;
				break;
			case IsolationLevel.ReadUncommitted:
				isolationLevel = System.Data.IsolationLevel.ReadUncommitted;
				break;
			case IsolationLevel.Snapshot:
				isolationLevel = System.Data.IsolationLevel.Snapshot;
				break;
			case IsolationLevel.Chaos:
				isolationLevel = System.Data.IsolationLevel.Chaos;
				break;
			case IsolationLevel.Unspecified:
			default:
				isolationLevel = System.Data.IsolationLevel.Unspecified;
				break;
			}
			m_transaction = Connection.BeginTransaction(isolationLevel);
		}

		protected override void OnPrepare(PreparingEnlistment enlistment)
		{
		}

		protected override void OnCommit(Enlistment enlistment)
		{
			m_transaction.Commit();
			m_transaction = null;
		}

		protected override void OnRollback(Enlistment enlistment)
		{
			m_transaction.Rollback();
			m_transaction = null;
		}

		MySqlTransaction m_transaction;
	}
}
#endif
