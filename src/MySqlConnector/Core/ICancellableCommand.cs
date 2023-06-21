using MySqlConnector.Utilities;

namespace MySqlConnector.Core;

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
	public static int GetNextId() => Interlocked.Increment(ref s_id);

	private static int s_id = 1;
}
