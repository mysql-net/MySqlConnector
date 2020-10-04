using System;

namespace MySqlConnector
{
	/// <summary>
	/// <see cref="MySqlConversionException"/> is thrown when a MySQL value can't be converted to another type.
	/// </summary>
	public class MySqlConversionException : Exception
	{
		/// <summary>
		/// Initializes a new instance of <see cref="MySqlConversionException"/>.
		/// </summary>
		/// <param name="message">The exception message.</param>
		public MySqlConversionException(string message)
			: base(message)
		{
		}
	}
}
