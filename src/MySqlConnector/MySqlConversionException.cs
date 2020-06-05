using System;

namespace MySqlConnector
{
	public class MySqlConversionException : Exception
	{
		public MySqlConversionException(string message)
			: base(message)
		{
		}
	}
}
