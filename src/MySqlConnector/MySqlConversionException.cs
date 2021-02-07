using System;
#if !NETSTANDARD1_3
using System.Runtime.Serialization;
#endif

namespace MySqlConnector
{
	/// <summary>
	/// <see cref="MySqlConversionException"/> is thrown when a MySQL value can't be converted to another type.
	/// </summary>
#if !NETSTANDARD1_3
	[Serializable]
#endif
	public class MySqlConversionException : Exception
	{
		/// <summary>
		/// Initializes a new instance of <see cref="MySqlConversionException"/>.
		/// </summary>
		/// <param name="message">The exception message.</param>
		internal MySqlConversionException(string message)
			: base(message)
		{
		}

#if !NETSTANDARD1_3
		private MySqlConversionException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
#endif
	}
}
