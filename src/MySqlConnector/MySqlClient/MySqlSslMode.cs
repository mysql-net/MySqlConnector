namespace MySql.Data.MySqlClient
{
	/// <summary>
	/// SSL connection options.
	/// </summary>
	public enum MySqlSslMode
	{
		/// <summary>
		/// Do not use SSL. This is the default.
		/// </summary>
		None,

		/// <summary>
		/// Always use SSL. Deny connection if server does not support SSL.
		/// </summary>
		Required,

		/// <summary>
		///  Always use SSL. Validate the Certificate Authority but tolerate name mismatch.
		/// </summary>
		VerifyCA,

		/// <summary>
		/// Always use SSL. Fail if the host name is not correct.
		/// </summary>
		VerifyFull,
	}
}
