using System.Runtime.Serialization;

namespace MySqlConnector;

/// <summary>
/// <see cref="MySqlConversionException"/> is thrown when a MySQL value can't be converted to another type.
/// </summary>
[Serializable]
public sealed class MySqlConversionException : Exception
{
	/// <summary>
	/// Initializes a new instance of <see cref="MySqlConversionException"/>.
	/// </summary>
	/// <param name="message">The exception message.</param>
	internal MySqlConversionException(string message)
		: base(message)
	{
	}

#if NET8_0_OR_GREATER
	[Obsolete(DiagnosticId = "SYSLIB0051")]
#endif
	private MySqlConversionException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
