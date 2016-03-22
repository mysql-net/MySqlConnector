using System;
using System.Data;
using System.Data.Common;

namespace MySql.Data.MySqlClient
{
	public class MySqlCommand : DbCommand
	{
		public override void Cancel()
		{
			throw new NotImplementedException();
		}

		protected override DbParameter CreateDbParameter()
		{
			throw new NotImplementedException();
		}

		protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
		{
			throw new NotImplementedException();
		}

		public override int ExecuteNonQuery()
		{
			throw new NotImplementedException();
		}

		public override object ExecuteScalar()
		{
			throw new NotImplementedException();
		}

		public override void Prepare()
		{
			throw new NotImplementedException();
		}

		public override string CommandText { get; set; }
		public override int CommandTimeout { get; set; }
		public override CommandType CommandType { get; set; }
		protected override DbConnection DbConnection { get; set; }
		protected override DbParameterCollection DbParameterCollection { get { throw new NotImplementedException(); } }
		protected override DbTransaction DbTransaction { get; set; }
		public override bool DesignTimeVisible { get; set; }
		public override UpdateRowSource UpdatedRowSource { get; set; }

		internal MySqlCommand(MySqlConnection connection)
		{
			m_connection = connection;
		}

		readonly MySqlConnection m_connection;
	}
}
