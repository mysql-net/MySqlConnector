using System.Diagnostics.Metrics;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core;

internal static class MetricsReporter
{
	public static void AddIdle(ConnectionPool pool) => s_connectionsUsageCounter.Add(1, pool.IdleStateTagList);
	public static void RemoveIdle(ConnectionPool pool) => s_connectionsUsageCounter.Add(-1, pool.IdleStateTagList);
	public static void AddUsed(ConnectionPool pool) => s_connectionsUsageCounter.Add(1, pool.UsedStateTagList);
	public static void RemoveUsed(ConnectionPool pool) => s_connectionsUsageCounter.Add(-1, pool.UsedStateTagList);
	public static void RecordCreateTime(ConnectionPool pool, float milliseconds) => s_createTimeHistory.Record(milliseconds, pool.PoolNameTagList);
	public static void RecordUseTime(ConnectionPool pool, float milliseconds) => s_useTimeHistory.Record(milliseconds, pool.PoolNameTagList);
	public static void RecordWaitTime(ConnectionPool pool, float milliseconds) => s_waitTimeHistory.Record(milliseconds, pool.PoolNameTagList);

	public static void AddPendingRequest(ConnectionPool? pool)
	{
		if (pool is not null)
			s_pendingRequestsCounter.Add(1, pool.PoolNameTagList);
	}

	public static void RemovePendingRequest(ConnectionPool? pool)
	{
		if (pool is not null)
			s_pendingRequestsCounter.Add(-1, pool.PoolNameTagList);
	}

	static MetricsReporter()
	{
		ActivitySourceHelper.Meter.CreateObservableUpDownCounter<int>("db.client.connections.idle.max",
			observeValues: GetMaximumConnections, unit: "{connection}",
			description: "The maximum number of idle open connections allowed; this corresponds to MaximumPoolSize in the connection string.");
		ActivitySourceHelper.Meter.CreateObservableUpDownCounter<int>("db.client.connections.idle.min",
			observeValues: GetMinimumConnections, unit: "{connection}",
			description: "The minimum number of idle open connections allowed; this corresponds to MinimumPoolSize in the connection string.");
		ActivitySourceHelper.Meter.CreateObservableUpDownCounter<int>("db.client.connections.max",
			observeValues: GetMaximumConnections, unit: "{connection}",
			description: "The maximum number of open connections allowed; this corresponds to MaximumPoolSize in the connection string.");

		static IEnumerable<Measurement<int>> GetMaximumConnections() =>
			ConnectionPool.GetAllPools().Select(x => new Measurement<int>(x.ConnectionSettings.MaximumPoolSize, x.PoolNameTagList));

		static IEnumerable<Measurement<int>> GetMinimumConnections() =>
			ConnectionPool.GetAllPools().Select(x => new Measurement<int>(x.ConnectionSettings.MinimumPoolSize, x.PoolNameTagList));
	}

	private static readonly UpDownCounter<int> s_connectionsUsageCounter = ActivitySourceHelper.Meter.CreateUpDownCounter<int>("db.client.connections.usage",
			unit: "{connection}", description: "The number of connections that are currently in the state described by the state tag.");
	private static readonly UpDownCounter<int> s_pendingRequestsCounter = ActivitySourceHelper.Meter.CreateUpDownCounter<int>("db.client.connections.pending_requests",
			unit: "{request}", description: "The number of pending requests for an open connection, cumulative for the entire pool.");
	private static readonly Histogram<float> s_createTimeHistory = ActivitySourceHelper.Meter.CreateHistogram<float>("db.client.connections.create_time",
			unit: "ms", description: "The time it took to create a new connection.");
	private static readonly Histogram<float> s_useTimeHistory = ActivitySourceHelper.Meter.CreateHistogram<float>("db.client.connections.use_time",
		unit: "ms", description: "The time between borrowing a connection and returning it to the pool.");
	private static readonly Histogram<float> s_waitTimeHistory = ActivitySourceHelper.Meter.CreateHistogram<float>("db.client.connections.wait_time",
			unit: "ms", description: "The time it took to obtain an open connection from the pool.");
}
