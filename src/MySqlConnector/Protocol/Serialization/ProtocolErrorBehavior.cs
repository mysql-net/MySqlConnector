namespace MySqlConnector.Protocol.Serialization
{
	/// <summary>
	/// Specifies how to handle protocol errors.
	/// </summary>
	internal enum ProtocolErrorBehavior
	{
		/// <summary>
		/// Throw an exception when there is a protocol error. This is the default.
		/// </summary>
		Throw,

		/// <summary>
		/// Ignore any protocol errors.
		/// </summary>
		Ignore,
	}
}
