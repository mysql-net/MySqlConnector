namespace MySql.Data.MySqlClient
{
	/// <summary>
	/// MySQL Server error codes. Taken from <a href="https://dev.mysql.com/doc/refman/5.7/en/error-messages-server.html">Server Error Codes and Messages</a>.
	/// </summary>
	public enum MySqlErrorCode
	{
		/// <summary>
		/// You have an error in your SQL syntax (ER_PARSE_ERROR).
		/// </summary>
		ParseError = 1064,

		/// <summary>
		/// Query execution was interrupted (ER_QUERY_INTERRUPTED).
		/// </summary>
		QueryInterrupted = 1317,
	}
}
