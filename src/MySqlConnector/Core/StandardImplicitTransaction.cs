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
			m_transaction = Connection.BeginTransaction();
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
