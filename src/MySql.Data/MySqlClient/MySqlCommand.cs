using System;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.Serialization;
using static System.FormattableString;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlCommand : DbCommand
	{
		public MySqlCommand()
			: this(null, null, null)
		{
		}

		public MySqlCommand(string commandText)
			: this(commandText, null, null)
		{
		}

		public MySqlCommand(MySqlConnection connection)
			: this(null, connection, null)
		{
		}

		public MySqlCommand(string commandText, MySqlConnection connection)
			: this(commandText, connection, null)
		{
		}

		public MySqlCommand(string commandText, MySqlConnection connection, MySqlTransaction transaction)
		{
			CommandText = commandText;
			DbConnection = connection;
			DbTransaction = transaction;
			m_parameterCollection = new MySqlParameterCollection();
		}

		public new MySqlParameterCollection Parameters
		{
			get
			{
				VerifyNotDisposed();
				return m_parameterCollection;
			}
		}

		public override void Cancel()
		{
			// documentation says this shouldn't throw (but just fail silently), but for now make it explicit that this doesn't work
			throw new NotSupportedException("Use the Async overloads with a CancellationToken.");
		}

		public override int ExecuteNonQuery()
			=> ExecuteNonQueryAsync(CancellationToken.None).GetAwaiter().GetResult();

		public override object ExecuteScalar()
			=> ExecuteNonQueryAsync(CancellationToken.None).GetAwaiter().GetResult();

		public override void Prepare()
		{
			// NOTE: Prepared statements in MySQL are not currently supported.
			// 1) Only a subset of statements are actually preparable by the server: http://dev.mysql.com/worklog/task/?id=2871
			// 2) Although CLIENT_MULTI_STATEMENTS is supposed to mean that the Server "Can handle multiple statements per COM_QUERY and COM_STMT_PREPARE" (https://dev.mysql.com/doc/internals/en/capability-flags.html#flag-CLIENT_MULTI_STATEMENTS),
			//    this is not actually true because "Prepared statement handles are defined to work only with strings that contain a single statement" (http://dev.mysql.com/doc/refman/5.7/en/c-api-multiple-queries.html).
		}

		public override string CommandText { get; set; }
		public override int CommandTimeout { get; set; }

		public override CommandType CommandType
		{
			get
			{
				return CommandType.Text;
			}
			set
			{
				if (value != CommandType.Text)
					throw new ArgumentException("CommandType must be Text.", nameof(value));
			}
		}

		public override bool DesignTimeVisible { get; set; }

		public override UpdateRowSource UpdatedRowSource
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		protected override DbConnection DbConnection { get; set; }
		protected override DbParameterCollection DbParameterCollection => m_parameterCollection;
		protected override DbTransaction DbTransaction { get; set; }

		protected override DbParameter CreateDbParameter()
		{
			VerifyNotDisposed();
			return new MySqlParameter();
		}

		protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
			=> ExecuteDbDataReaderAsync(behavior, CancellationToken.None).GetAwaiter().GetResult();

		public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
		{
			using (var reader = await ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
			{
				do
				{
					while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
					{
					}
				} while (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false));
				return reader.RecordsAffected;
			}
		}

		public override async Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
		{
			using (var reader = await ExecuteReaderAsync(CommandBehavior.SingleResult | CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false))
			{
				do
				{
					if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
						return reader.GetValue(0);
				} while (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false));
			}
			return null;
		}

		protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
		{
			VerifyValid();

			var preparer = new MySqlStatementPreparer(CommandText, m_parameterCollection);
			preparer.BindParameters();
			var payload = new PayloadData(new ArraySegment<byte>(Payload.CreateEofStringPayload(CommandKind.Query, preparer.PreparedSql)));
			await Session.SendAsync(payload, cancellationToken);
			return await MySqlDataReader.CreateAsync(this, behavior, cancellationToken);
		}

		protected override void Dispose(bool disposing)
		{
			try
			{
				if (disposing)
				{
					m_parameterCollection = null;
				}
			}
			finally
			{
				base.Dispose(disposing);
			}
		}

		private MySqlSession Session => ((MySqlConnection) DbConnection).Session;

		private void VerifyNotDisposed()
		{
			if (m_parameterCollection == null)
				throw new ObjectDisposedException(GetType().Name);
		}

		private void VerifyValid()
		{
			VerifyNotDisposed();
			if (DbConnection == null)
				throw new InvalidOperationException("Connection property must be non-null.");
			if (DbConnection.State != ConnectionState.Open && DbConnection.State != ConnectionState.Connecting)
				throw new InvalidOperationException(Invariant($"Connection must be Open; current state is {DbConnection.State}"));
			// TODO: if (DbTransaction != ((MySqlConnection) DbConnection).CurrentTransaction)
			//	throw new InvalidOperationException("The transaction associated with this command is not the connection's active transaction.");
			if (string.IsNullOrWhiteSpace(CommandText))
				throw new InvalidOperationException("CommandText must be specified");
		}

		MySqlParameterCollection m_parameterCollection;
	}
}
