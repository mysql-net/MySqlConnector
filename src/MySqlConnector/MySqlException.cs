using System;
using System.Collections;
using System.Data.Common;
#if !NETSTANDARD1_3
using System.Runtime.Serialization;
#endif

namespace MySqlConnector
{
	/// <summary>
	/// <see cref="MySqlException"/> is thrown when MySQL Server returns an error code, or there is a
	/// communication error with the server.
	/// </summary>
#if !NETSTANDARD1_3
	[Serializable]
#endif
	public sealed class MySqlException : DbException
	{
		/// <summary>
		/// A <see cref="MySqlErrorCode"/> value identifying the kind of error. Prefer to use the <see cref="ErrorCode"/> property.
		/// </summary>
		public int Number { get; }

		/// <summary>
		/// A <see cref="MySqlErrorCode"/> value identifying the kind of error.
		/// </summary>
#if NETSTANDARD1_3
		public MySqlErrorCode ErrorCode { get; }
#else
		public new MySqlErrorCode ErrorCode { get; }
#endif

		/// <summary>
		/// A <c>SQLSTATE</c> code identifying the kind of error.
		/// </summary>
		/// <remarks>See <a href="https://en.wikipedia.org/wiki/SQLSTATE">SQLSTATE</a> for more information.</remarks>
#if NET45 || NET461 || NET471 || NETSTANDARD1_3 || NETSTANDARD2_0 || NETSTANDARD2_1 || NETCOREAPP2_1 || NETCOREAPP3_1
		public string? SqlState { get; }
#else
		public override string? SqlState { get; }
#endif

#if !NETSTANDARD1_3
		private MySqlException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			Number = info.GetInt32("Number");
			SqlState = info.GetString("SqlState");
		}

		/// <summary>
		/// Sets the <see cref="SerializationInfo"/> with information about the exception.
		/// </summary>
		/// <param name="info">The <see cref="SerializationInfo"/> that will be set.</param>
		/// <param name="context">The context.</param>
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Number", Number);
			info.AddValue("SqlState", SqlState);
		}
#endif

		/// <summary>
		/// Gets a collection of key/value pairs that provide additional information about the exception.
		/// </summary>
		public override IDictionary Data
		{
			get
			{
				if (m_data is null)
				{
					m_data = base.Data;
					m_data["Server Error Code"] = Number;
					m_data["SqlState"] = SqlState;
				}
				return m_data;
			}
		}

		internal MySqlException(string message)
			: this(message, null)
		{
		}

		internal MySqlException(string message, Exception? innerException)
			: this(default, null, message, innerException)
		{
		}

		internal MySqlException(MySqlErrorCode errorCode, string message)
			: this(errorCode, null, message, null)
		{
		}

		internal MySqlException(MySqlErrorCode errorCode, string message, Exception? innerException)
			: this(errorCode, null, message, innerException)
		{
		}

		internal MySqlException(MySqlErrorCode errorCode, string sqlState, string message)
			: this(errorCode, sqlState, message, null)
		{
		}

		internal MySqlException(MySqlErrorCode errorCode, string? sqlState, string message, Exception? innerException)
			: base(message, innerException)
		{
			ErrorCode = errorCode;
			Number = (int) errorCode;
			SqlState = sqlState;
		}

		internal static MySqlException CreateForTimeout() => CreateForTimeout(null);

		internal static MySqlException CreateForTimeout(Exception? innerException) =>
			new MySqlException(MySqlErrorCode.CommandTimeoutExpired, "The Command Timeout expired before the operation completed.", innerException);

		IDictionary? m_data;
	}
}
