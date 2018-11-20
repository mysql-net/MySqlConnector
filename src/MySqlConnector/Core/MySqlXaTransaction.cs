#if !NETSTANDARD1_3
using System;
using System.Globalization;
using System.Threading;
using System.Transactions;
using MySql.Data.MySqlClient;

namespace MySqlConnector.Core
{
	internal sealed class MySqlXaTransaction : IEnlistmentNotification
	{
		public MySqlXaTransaction(MySqlConnection connection) => Connection = connection;

		public MySqlConnection Connection { get; }

		public void Start(Transaction transaction)
		{
			m_transaction = transaction;

			// generate an "xid" with "gtrid" (Global TRansaction ID) from the .NET Transaction and "bqual" (Branch QUALifier)
			// unique to this object
			var id = Interlocked.Increment(ref s_currentId);
			m_xid = "'" + transaction.TransactionInformation.LocalIdentifier + "', '" + id.ToString(CultureInfo.InvariantCulture) + "'";

			ExecuteXaCommand("START");

			// TODO: Support EnlistDurable and enable recovery via "XA RECOVER"
			transaction.EnlistVolatile(this, EnlistmentOptions.None);
		}

		public void Prepare(PreparingEnlistment enlistment)
		{
			ExecuteXaCommand("END");
			ExecuteXaCommand("PREPARE");
			enlistment.Prepared();
		}

		public void Commit(Enlistment enlistment)
		{
			ExecuteXaCommand("COMMIT");
			enlistment.Done();
			Connection.UnenlistTransaction(this, m_transaction);
			m_transaction = null;
		}

		public void Rollback(Enlistment enlistment)
		{
			ExecuteXaCommand("END");
			ExecuteXaCommand("ROLLBACK");
			enlistment.Done();
			Connection.UnenlistTransaction(this, m_transaction);
			m_transaction = null;
		}

		public void InDoubt(Enlistment enlistment) => throw new NotSupportedException();

		private void ExecuteXaCommand(string statement)
		{
			using (var cmd = Connection.CreateCommand())
			{
				cmd.CommandText = "XA " + statement + " " + m_xid;
				cmd.ExecuteNonQuery();
			}
		}

		static int s_currentId;

		Transaction m_transaction;
		string m_xid;
	}
}
#endif
