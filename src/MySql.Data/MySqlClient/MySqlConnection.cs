using System;
using System.Data;
using System.Data.Common;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.Serialization;
using static System.FormattableString;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlConnection : DbConnection
	{
		public MySqlConnection()
		{
		}

		public MySqlConnection(string connectionString)
			: this()
		{
			ConnectionString = connectionString;
		}

		public new MySqlTransaction BeginTransaction()
		{
			return (MySqlTransaction) base.BeginTransaction();
		}

		protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
		{
			if (State != ConnectionState.Open)
				throw new InvalidOperationException("Connection is not open.");
			if (CurrentTransaction != null)
				throw new InvalidOperationException("Transactions may not be nested.");

			string isolationLevelValue;
			switch (isolationLevel)
			{
			case IsolationLevel.ReadUncommitted:
				isolationLevelValue = "read uncommitted";
				break;

			case IsolationLevel.ReadCommitted:
				isolationLevelValue = "read committed";
				break;

			case IsolationLevel.Unspecified:
			// "In terms of the SQL:1992 transaction isolation levels, the default InnoDB level is REPEATABLE READ." - http://dev.mysql.com/doc/refman/5.7/en/innodb-transaction-model.html
			case IsolationLevel.RepeatableRead:
				isolationLevelValue = "repeatable read";
				break;

			case IsolationLevel.Serializable:
				isolationLevelValue = "serializable";
				break;

			case IsolationLevel.Chaos:
			case IsolationLevel.Snapshot:
			default:
				throw new NotSupportedException(Invariant($"IsolationLevel.{isolationLevel} is not supported."));
			}

			using (var cmd = new MySqlCommand("set session transaction isolation level " + isolationLevelValue + "; start transaction;", this))
				cmd.ExecuteNonQuery();

			var transaction = new MySqlTransaction(this, isolationLevel);
			CurrentTransaction = transaction;
			return transaction;
		}

		public override void Close()
		{
			if (CurrentTransaction != null)
			{
				CurrentTransaction.Dispose();
				CurrentTransaction = null;
			}
			Utility.Dispose(ref m_session);
			SetState(ConnectionState.Closed);
			m_isDisposed = true;
		}

		public override void ChangeDatabase(string databaseName)
		{
			throw new NotImplementedException();
		}

		public override void Open()
		{
			OpenAsync(CancellationToken.None).GetAwaiter().GetResult();
		}

		public override async Task OpenAsync(CancellationToken cancellationToken)
		{
			VerifyNotDisposed();
			if (State != ConnectionState.Closed)
				throw new InvalidOperationException(Invariant($"Cannot Open when State is {State}."));

			var connectionStringBuilder = new MySqlConnectionStringBuilder { ConnectionString = ConnectionString };
			m_database = connectionStringBuilder.Database;

			SetState(ConnectionState.Connecting);

			bool success = false;
			try
			{
				m_session = new MySqlSession();
				await m_session.ConnectAsync(connectionStringBuilder.Server, (int) connectionStringBuilder.Port).ConfigureAwait(false);
				var payload = await m_session.ReceiveAsync(cancellationToken).ConfigureAwait(false);
				var reader = new ByteArrayReader(payload.ArraySegment.Array, payload.ArraySegment.Offset, payload.ArraySegment.Count);
				var initialHandshake = new InitialHandshakePacket(reader);
				m_serverVersion = Encoding.ASCII.GetString(initialHandshake.ServerVersion);

				var response = HandshakeResponse41Packet.Create(initialHandshake, connectionStringBuilder.UserID, connectionStringBuilder.Password, connectionStringBuilder.Database);
				payload = new PayloadData(new ArraySegment<byte>(response));
				await m_session.SendReplyAsync(payload, cancellationToken).ConfigureAwait(false);
				await m_session.ReceiveReplyAsync(cancellationToken).ConfigureAwait(false);
				// TODO: Check success

				SetState(ConnectionState.Open);
				success = true;
			}
			catch (SocketException ex)
			{
				SetState(ConnectionState.Closed);
				throw new MySqlException("Unable to connect to any of the specified MySQL hosts.", ex);
			}
			finally
			{
				if (!success)
					Utility.Dispose(ref m_session);
			}
		}

		public override string ConnectionString { get; set; }

		public override string Database => m_database;

		public override ConnectionState State => m_connectionState;

		public override string DataSource => m_database;

		public override string ServerVersion => m_serverVersion;

		protected override DbCommand CreateDbCommand()
		{
			return new MySqlCommand(this);
		}

		public override int ConnectionTimeout
		{
			get { throw new NotImplementedException(); }
		}

		internal MySqlSession Session
		{
			get
			{
				VerifyNotDisposed();
				return m_session;
			}
		}

		internal MySqlTransaction CurrentTransaction { get; set; }

		private void SetState(ConnectionState newState)
		{
			if (m_connectionState != newState)
			{
				var previousState = m_connectionState;
				m_connectionState = newState;
				OnStateChange(new StateChangeEventArgs(previousState, newState));
			}
		}

		private void VerifyNotDisposed()
		{
			if (m_isDisposed)
				throw new ObjectDisposedException(GetType().Name);
		}

		MySqlSession m_session;
		ConnectionState m_connectionState;
		bool m_isDisposed;
		string m_database;
		string m_serverVersion;
	}
}
