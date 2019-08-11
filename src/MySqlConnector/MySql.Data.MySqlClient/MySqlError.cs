namespace MySql.Data.MySqlClient
{
	public sealed class MySqlError
	{
		internal MySqlError(string level, int code, string message)
		{
			Level = level;
			Code = code;
			Message = message;
		}

		public string Level { get; }
		public int Code { get; }
		public string Message { get; }
	};
}
