using System;
using System.Data.Common;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlException : DbException
	{
		public int ErrorNumber { get; }
		public string SqlState { get; }

		internal MySqlException(int errorNumber, string sqlState, string message)
			: this(errorNumber, sqlState, message, null)
		{
		}

		internal MySqlException(int errorNumber, string sqlState, string message, Exception innerException)
			: base(message, innerException)
		{
			ErrorNumber = errorNumber;
			SqlState = sqlState;
		}
	}
}
