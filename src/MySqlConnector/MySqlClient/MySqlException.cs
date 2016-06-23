using System;
using System.Data.Common;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlException : DbException
	{
		public int Number { get; }
		public string SqlState { get; }

		internal MySqlException(string message)
			: this(message, null)
		{
		}

		internal MySqlException(string message, Exception innerException)
			: this(0, null, message, innerException)
		{
		}

		internal MySqlException(int errorNumber, string sqlState, string message)
			: this(errorNumber, sqlState, message, null)
		{
		}

		internal MySqlException(int errorNumber, string sqlState, string message, Exception innerException)
			: base(message, innerException)
		{
			Number = errorNumber;
			SqlState = sqlState;
		}
	}
}
