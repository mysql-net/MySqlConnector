using System;
using System.Threading;
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
		MySqlConnection? Connection { get; }
		IDisposable? RegisterCancel(CancellationToken cancellationToken);
		void SetTimeout(int milliseconds);
		bool IsTimedOut { get; }
	}

	internal static class ICancellableCommandExtensions
	{
		/// <summary>
		/// Returns a unique ID for all implementations of <see cref="ICancellableCommand"/>.
		/// </summary>
		/// <returns></returns>
		public static int GetNextId() => Interlocked.Increment(ref s_id);

		/// <summary>
		/// Returns the time (in seconds) until a command should be canceled, clamping it to the maximum time
		/// allowed including CancellationTimeout.
		/// </summary>
		public static int GetCommandTimeUntilCanceled(this ICancellableCommand command)
		{
			var commandTimeout = command.CommandTimeout;
			var session = command.Connection?.Session;
			if (commandTimeout == 0 || session is null)
				return 0;

			// the total cancellation period (graphically) is [===CommandTimeout===][=CancellationTimeout=], which can't
			// exceed int.MaxValue/1000 because it has to be multiplied by 1000 to be converted to milliseconds
			return Math.Min(commandTimeout, Math.Max(1, (int.MaxValue / 1000) - session.CancellationTimeout));
		}

		/// <summary>
		/// Causes the effective command timeout to be reset back to the value specified by <see cref="ICancellableCommand.CommandTimeout"/>
		/// plus <see cref="MySqlConnectionStringBuilder.CancellationTimeout"/>. This allows for the command to time out, a cancellation to attempt
		/// to happen, then the "hard" timeout to occur.
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
			var session = command.Connection?.Session;
			if (session is not null)
			{
				if (command.CommandTimeout == 0 || session.CancellationTimeout == 0)
				{
					session.SetTimeout(Constants.InfiniteTimeout);
				}
				else
				{
					var commandTimeUntilCanceled = command.GetCommandTimeUntilCanceled() * 1000;
					if (session.CancellationTimeout > 0)
					{
						// try to cancel first, then close socket
						command.SetTimeout(commandTimeUntilCanceled);
						session.SetTimeout(commandTimeUntilCanceled + session.CancellationTimeout * 1000);
					}
					else
					{
						// close socket once the timeout is reached
						session.SetTimeout(commandTimeUntilCanceled);
					}
				}
			}
		}

		static int s_id = 1;
	}
}
