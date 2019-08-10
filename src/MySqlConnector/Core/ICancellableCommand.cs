#nullable disable
using System;
using System.Threading;
using MySql.Data.MySqlClient;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core
{
	/// <summary>
	/// <see cref="IMySqlCommand"/> provides an internal abstraction over operations that can be cancelled: <see cref="MySqlCommand"/> and <see cref="MySqlBatch"/>.
	/// </summary>
	internal interface ICancellableCommand
	{
		int CommandId { get; }
		int CommandTimeout { get; }
		int CancelAttemptCount { get; set; }
		MySqlConnection Connection { get; }
		IDisposable RegisterCancel(CancellationToken cancellationToken);
	}

	internal static class ICancellableCommandExtensions
	{
		/// <summary>
		/// Returns a unique ID for all implementations of <see cref="ICancellableCommand"/>.
		/// </summary>
		/// <returns></returns>
		public static int GetNextId() => Interlocked.Increment(ref s_id);

		/// <summary>
		/// Causes the effective command timeout to be reset back to the value specified by <see cref="CommandTimeout"/>.
		/// </summary>
		/// <remarks>As per the <a href="https://msdn.microsoft.com/en-us/library/system.data.sqlclient.sqlcommand.commandtimeout.aspx">MSDN documentation</a>,
		/// "This property is the cumulative time-out (for all network packets that are read during the invocation of a method) for all network reads during command
		/// execution or processing of the results. A time-out can still occur after the first row is returned, and does not include user processing time, only network
		/// read time. For example, with a 30 second time out, if Read requires two network packets, then it has 30 seconds to read both network packets. If you call
		/// Read again, it will have another 30 seconds to read any data that it requires."
		/// The <see cref="ResetCommandTimeout"/> method is called by public ADO.NET API methods to reset the effective time remaining at the beginning of a new
		/// method call.</remarks>
		public static void ResetCommandTimeout(this ICancellableCommand command)
		{
			var commandTimeout = command.CommandTimeout;
			command.Connection?.Session?.SetTimeout(commandTimeout == 0 ? Constants.InfiniteTimeout : commandTimeout * 1000);
		}

		static int s_id = 1;
	}
}
