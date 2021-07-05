using System;

namespace MySqlConnector
{
	/// <summary>
	/// <see cref="MySqlError"/> represents an error or warning that occurred during the execution of a SQL statement.
	/// </summary>
	public sealed class MySqlError
	{
		internal MySqlError(string level, int code, string message)
		{
			Level = level;
#pragma warning disable 618
			Code = code;
#pragma warning restore
			ErrorCode = (MySqlErrorCode) code;
			Message = message;
		}

		/// <summary>
		/// The error level. This comes from the MySQL Server. Possible values include <c>Note</c>, <c>Warning</c>, and <c>Error</c>.
		/// </summary>
		public string Level { get; }

		/// <summary>
		/// The numeric error code. Prefer to use <see cref="ErrorCode"/>.
		/// </summary>
		[Obsolete("Use ErrorCode")]
		public int Code { get; }

		/// <summary>
		/// The <see cref="MySqlErrorCode"/> for the error or warning.
		/// </summary>
		public MySqlErrorCode ErrorCode { get; }

		/// <summary>
		/// A human-readable description of the error or warning.
		/// </summary>
		public string Message { get; }
	};
}
