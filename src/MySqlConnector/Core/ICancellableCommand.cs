using MySqlConnector.Utilities;

namespace MySqlConnector.Core;

/// <summary>
/// <see cref="IMySqlCommand"/> provides an internal abstraction over operations that can be cancelled: <see cref="MySqlCommand"/> and <see cref="MySqlBatch"/>.
/// </summary>
internal interface ICancellableCommand
{
	int CommandId { get; }
	int CommandTimeout { get; }
	int? EffectiveCommandTimeout { get; set; }
	int CancelAttemptCount { get; set; }
	MySqlConnection? Connection { get; }
	CancellationTokenRegistration RegisterCancel(CancellationToken cancellationToken);
	void SetTimeout(int milliseconds);
	bool IsTimedOut { get; }
}

internal static class ICancellableCommandExtensions
{
	/// <summary>
	/// Returns a unique ID for all implementations of <see cref="ICancellableCommand"/>.
	/// </summary>
	public static int GetNextId() => Interlocked.Increment(ref s_id);

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
		// read value cached on the command
		var effectiveCommandTimeout = command.EffectiveCommandTimeout;

		// early out if there is no timeout
		if (effectiveCommandTimeout == Constants.InfiniteTimeout)
			return;

		var session = command.Connection?.Session;
		if (session is null)
			return;

		// determine the effective command timeout if not already cached
		if (effectiveCommandTimeout is null)
		{
			var commandTimeout = command.CommandTimeout;
			var cancellationTimeout = session.CancellationTimeout;

			if (commandTimeout == 0 || cancellationTimeout == 0)
			{
				// if commandTimeout is zero, then cancellation doesn't occur
				effectiveCommandTimeout = Constants.InfiniteTimeout;
			}
			else
			{
				// the total cancellation period (graphically) is [===CommandTimeout===][=CancellationTimeout=], which can't
				// exceed int.MaxValue/1000 because it has to be multiplied by 1000 to be converted to milliseconds
				effectiveCommandTimeout = Math.Min(commandTimeout, Math.Max(1, (int.MaxValue / 1000) - Math.Max(0, session.CancellationTimeout))) * 1000;
			}

			command.EffectiveCommandTimeout = effectiveCommandTimeout;
		}

		if (effectiveCommandTimeout == Constants.InfiniteTimeout)
		{
			// for no timeout, we set an infinite timeout once (then early out above)
			session.SetTimeout(Constants.InfiniteTimeout);
		}
		else if (session.CancellationTimeout > 0)
		{
			// try to cancel first, then close socket
			command.SetTimeout(effectiveCommandTimeout.Value);
			session.SetTimeout(effectiveCommandTimeout.Value + (session.CancellationTimeout * 1000));
		}
		else
		{
			// close socket once the timeout is reached
			session.SetTimeout(effectiveCommandTimeout.Value);
		}
	}

	private static int s_id = 1;
}
