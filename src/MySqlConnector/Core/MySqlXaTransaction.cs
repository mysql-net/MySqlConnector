#if !NETSTANDARD1_3
using System;
using System.Globalization;
using System.Threading;
using System.Transactions;

namespace MySql.Data.MySqlClient
{
	internal sealed class MySqlXaTransaction : IEnlistmentNotification
	{
		public MySqlXaTransaction(MySqlConnection connection) => m_connection = connection;

		public void Start(Transaction transaction)
		{
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
			m_connection.UnenlistTransaction(this);
		}

		public void Rollback(Enlistment enlistment)
		{
			ExecuteXaCommand("END");
			ExecuteXaCommand("ROLLBACK");
			enlistment.Done();
			m_connection.UnenlistTransaction(this);
		}

		public void InDoubt(Enlistment enlistment) => throw new NotSupportedException();

		private void ExecuteXaCommand(string statement)
		{
			using (var cmd = m_connection.CreateCommand())
			{
				cmd.CommandText = "XA " + statement + " " + m_xid;
				cmd.ExecuteNonQuery();
			}
		}

		static int s_currentId;

		readonly MySqlConnection m_connection;
		string m_xid;
	}
}
#endif
