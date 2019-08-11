using System;

namespace MySql.Data.Types
{
	public class MySqlConversionException : Exception
	{
		public MySqlConversionException(string message)
			: base(message)
		{
		}
	}
}
