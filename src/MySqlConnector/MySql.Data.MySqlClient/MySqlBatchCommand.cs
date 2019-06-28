using System.Data;
using System.Data.Common;
using MySqlConnector.Core;

#if NET45 || NET461 || NET471 || NETSTANDARD1_3 || NETSTANDARD2_0 || NETCOREAPP2_1 || NETCOREAPP3_0
namespace System.Data.Common
{
	public abstract class DbBatchCommand
	{
		public abstract string CommandText { get; set; }
		public abstract CommandType CommandType { get; set; }
		public abstract CommandBehavior CommandBehavior { get; set; }
		public abstract int RecordsAffected { get; set; }

		public DbParameterCollection Parameters => DbParameterCollection;
		protected abstract DbParameterCollection DbParameterCollection { get; }
	}
}
#endif

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlBatchCommand : DbBatchCommand, IMySqlCommand
	{
		public MySqlBatchCommand()
			: this(null)
		{
		}

		public MySqlBatchCommand(string commandText)
		{
			CommandText = commandText;
			CommandType = CommandType.Text;
		}

		public override string CommandText { get; set; }
		public override CommandType CommandType { get; set; }
		public override CommandBehavior CommandBehavior { get; set; }
		public override int RecordsAffected { get; set; }
		protected override DbParameterCollection DbParameterCollection => Parameters;

		public new MySqlParameterCollection Parameters
		{
			get
			{
				if (m_parameterCollection is null)
					m_parameterCollection = new MySqlParameterCollection();
				return m_parameterCollection;
			}
		}

		MySqlParameterCollection IMySqlCommand.RawParameters => m_parameterCollection;

		MySqlConnection IMySqlCommand.Connection => Batch.Connection;

		long IMySqlCommand.LastInsertedId => m_lastInsertedId;

		PreparedStatements IMySqlCommand.TryGetPreparedStatements() => null;

		void IMySqlCommand.SetLastInsertedId(long lastInsertedId) => m_lastInsertedId = lastInsertedId;

		MySqlParameterCollection IMySqlCommand.OutParameters { get; set; }

		MySqlParameter IMySqlCommand.ReturnParameter { get; set; }

		ICancellableCommand IMySqlCommand.CancellableCommand => Batch;

		internal MySqlBatch Batch { get; set; }

		MySqlParameterCollection m_parameterCollection;
		long m_lastInsertedId;
	}
}
