namespace MySqlConnector;

#pragma warning disable CA1027 // Mark enums with FlagsAttribute
#pragma warning disable CA1069 // Enum values should not be duplicated
/// <summary>
/// Specifies the type of connection to make to the server.
/// </summary>
public enum MySqlConnectionProtocol
{
	/// <summary>
	/// TCP/IP connection.
	/// </summary>
	Sockets = 1,
	Socket = 1,
	Tcp = 1,

	/// <summary>
	/// Named pipe connection. Only works on Windows.
	/// </summary>
	Pipe = 2,
	NamedPipe = 2,

	/// <summary>
	/// Unix domain socket connection. Only works on Unix/Linux.
	/// </summary>
	UnixSocket = 3,
	Unix = 3,

	/// <summary>
	/// Shared memory connection. Not currently supported.
	/// </summary>
	SharedMemory = 4,
	Memory = 4,
}
#pragma warning restore CA1069 // Enum values should not be duplicated
