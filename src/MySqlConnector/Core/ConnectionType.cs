namespace MySql.Data.Serialization
{
	/// <summary>
	/// Specifies whether to perform synchronous or asynchronous I/O.
	/// </summary>
	internal enum ConnectionType
	{
		/// <summary>
		/// Connection is a TCP connection.
		/// </summary>
		Tcp,

		/// <summary>
		/// Connection is a Unix Domain Socket.
		/// </summary>
		Unix,
	}
}
