using System.Diagnostics.Metrics;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core;

internal static class MetricsReporter
{
	public static void AddIdle(ConnectionPool pool) => s_connectionsUsageCounter.Add(1, pool.IdleStateTagList);
	public static void RemoveIdle(ConnectionPool pool) => s_connectionsUsageCounter.Add(-1, pool.IdleStateTagList);
	public static void AddUsed(ConnectionPool pool) => s_connectionsUsageCounter.Add(1, pool.UsedStateTagList);
	public static void RemoveUsed(ConnectionPool pool) => s_connectionsUsageCounter.Add(-1, pool.UsedStateTagList);
	public static void AddTimeout(ConnectionPool? pool, ConnectionSettings connectionSettings) => s_connectionTimeouts.Add(1, new KeyValuePair<string, object?>("pool.name", pool?.Name ?? connectionSettings.ApplicationName ?? connectionSettings.ConnectionStringBuilder.GetConnectionString(includePassword: false)));
	public static void RecordCreateTime(ConnectionPool pool, double seconds) => s_createTimeHistory.Record(seconds, pool.PoolNameTagList);
	public static void RecordUseTime(ConnectionPool pool, double seconds) => s_useTimeHistory.Record(seconds, pool.PoolNameTagList);
	public static void RecordWaitTime(ConnectionPool pool, double seconds) => s_waitTimeHistory.Record(seconds, pool.PoolNameTagList);

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
		_ = ActivitySourceHelper.Meter.CreateObservableUpDownCounter<int>("db.client.connections.idle.max",
			observeValues: GetMaximumConnections, unit: "{connection}",
			description: "The maximum number of idle open connections allowed; this corresponds to MaximumPoolSize in the connection string.");
		_ = ActivitySourceHelper.Meter.CreateObservableUpDownCounter<int>("db.client.connections.idle.min",
			observeValues: GetMinimumConnections, unit: "{connection}",
			description: "The minimum number of idle open connections allowed; this corresponds to MinimumPoolSize in the connection string.");
		_ = ActivitySourceHelper.Meter.CreateObservableUpDownCounter<int>("db.client.connections.max",
			observeValues: GetMaximumConnections, unit: "{connection}",
			description: "The maximum number of open connections allowed; this corresponds to MaximumPoolSize in the connection string.");

		static IEnumerable<Measurement<int>> GetMaximumConnections() =>
			ConnectionPool.GetAllPools().Select(static x => new Measurement<int>(x.ConnectionSettings.MaximumPoolSize, x.PoolNameTagList));

		static IEnumerable<Measurement<int>> GetMinimumConnections() =>
			ConnectionPool.GetAllPools().Select(static x => new Measurement<int>(x.ConnectionSettings.MinimumPoolSize, x.PoolNameTagList));
	}

	private static readonly UpDownCounter<int> s_connectionsUsageCounter = ActivitySourceHelper.Meter.CreateUpDownCounter<int>("db.client.connections.usage",
			unit: "{connection}", description: "The number of connections that are currently in the state described by the state tag.");
	private static readonly UpDownCounter<int> s_pendingRequestsCounter = ActivitySourceHelper.Meter.CreateUpDownCounter<int>("db.client.connections.pending_requests",
			unit: "{request}", description: "The number of pending requests for an open connection, cumulative for the entire pool.");
	private static readonly Counter<int> s_connectionTimeouts = ActivitySourceHelper.Meter.CreateCounter<int>("db.client.connections.timeouts",
			unit: "{timeout}", description: "The number of connection timeouts that have occurred trying to obtain a connection from the pool.");
	private static readonly Histogram<double> s_createTimeHistory = ActivitySourceHelper.Meter.CreateHistogram<double>("db.client.connections.create_time",
			unit: "s", description: "The time it took to create a new connection.");
	private static readonly Histogram<double> s_useTimeHistory = ActivitySourceHelper.Meter.CreateHistogram<double>("db.client.connections.use_time",
		unit: "s", description: "The time between borrowing a connection and returning it to the pool.");
	private static readonly Histogram<double> s_waitTimeHistory = ActivitySourceHelper.Meter.CreateHistogram<double>("db.client.connections.wait_time",
			unit: "s", description: "The time it took to obtain an open connection from the pool.");
}
