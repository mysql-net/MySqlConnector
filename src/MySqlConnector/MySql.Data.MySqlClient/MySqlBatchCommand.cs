using System.Data;
using MySqlConnector.Core;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlBatchCommand : IMySqlCommand
	{
		public MySqlBatchCommand()
			: this(null)
		{
		}

		public MySqlBatchCommand(string? commandText)
		{
			CommandText = commandText;
			CommandType = CommandType.Text;
		}

		public string? CommandText { get; set; }
		public CommandType CommandType { get; set; }
		public CommandBehavior CommandBehavior { get; set; }
		public int RecordsAffected { get; set; }

		public MySqlParameterCollection Parameters
		{
			get
			{
				if (m_parameterCollection is null)
					m_parameterCollection = new MySqlParameterCollection();
				return m_parameterCollection;
			}
		}

		MySqlParameterCollection? IMySqlCommand.RawParameters => m_parameterCollection;

		MySqlConnection? IMySqlCommand.Connection => Batch?.Connection;

		long IMySqlCommand.LastInsertedId => m_lastInsertedId;

		PreparedStatements? IMySqlCommand.TryGetPreparedStatements() => null;

		void IMySqlCommand.SetLastInsertedId(long lastInsertedId) => m_lastInsertedId = lastInsertedId;

		MySqlParameterCollection? IMySqlCommand.OutParameters { get; set; }

		MySqlParameter? IMySqlCommand.ReturnParameter { get; set; }

		ICancellableCommand IMySqlCommand.CancellableCommand => Batch!;

		internal MySqlBatch? Batch { get; set; }

		MySqlParameterCollection? m_parameterCollection;
		long m_lastInsertedId;
	}
}
