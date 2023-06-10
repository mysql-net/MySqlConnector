# ADO.NET Metrics Specification

## Introduction

This specification describes how to add metrics (following the
[OpenTelemetry Semantic Conventions for Database Metrics](https://github.com/open-telemetry/semantic-conventions/blob/main/specification/metrics/semantic_conventions/database-metrics.md))
to ADO.NET providers using [System.Diagnostics.Metrics](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics).

## Background

[System.Diagnostics.Metrics APIs](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/compare-metric-apis#systemdiagnosticsmetrics) are the newest cross-platform APIs for collecting performance metrics,
and were designed in close collaboration with the OpenTelemetry project.
OpenTelemetry semantic conventions are standards for generating consistent,
accessible telemetry across different databases and client libraries.

## General

This specification should be read in conjunction with the [OpenTelemetry Semantic Conventions](https://github.com/open-telemetry/semantic-conventions/blob/main/specification/metrics/semantic_conventions/database-metrics.md).
The OpenTelemetry Semantic Conventions MUST be followed except where this specification explicitly overrides them.

ADO.NET Providers MUST use the [`System.Diagnostics.Metrics.Meter` class](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.meter) to expose metrics.
It MUST be created with the name of the library and its version:

```csharp
public static Meter Meter { get; } = new("Xyzsql", "1.0.0");
```

## Pool Names

OpenTelemetry Semantic Conventions assume that connection pools can be named.
ADO.NET Providers SHOULD use the `DbDataSource` class to expose a way to create named connection pools.
It is recommended that a `XyzsqlDataSourceBuilder` class be provided to construct DbDataSource instances,
and that it have a `UseName(string name)` method to set the connection pool name.
Client code could be written as follows:

```csharp
using var dataSource =
  new XyzsqlDataSourceBuilder(connectionString)
    .UseName(“My Pool”)
    .Build();

// Metrics for this connection have the name "My Pool" associated with them.
using var connection = dataSource.OpenConnection();
```

Or, a dependency injection helper library might expose it as follows:

```csharp
builder.Services.AddXyzsqlDataSource(connectionString,
  x => x.UseName("My Pool"));
```

If the user does not specify a pool name, the ADO.NET Provider MUST use the connection string (without a password) as the pool name in all reported metrics.

The pool name MUST be associated with a metric by adding a `pool.name` tag whenever a metric is reported via an `Instrument`.

## Instruments

A straightforward translation of the [OpenTelemetry Instruments for Connection Pools](https://github.com/open-telemetry/semantic-conventions/blob/main/specification/metrics/semantic_conventions/database-metrics.md#connection-pools) into C# is:

```csharp
static readonly UpDownCounter<int> ConnectionsUsageCounter = Meter.CreateUpDownCounter<int>("db.client.connections.usage",
  unit: "{connection}", description: "The number of connections that are currently in the state described by the state tag.");
static readonly UpDownCounter<int> MaxIdleConnectionsCounter = Meter.CreateUpDownCounter<int>("db.client.connections.idle.max",
  unit: "{connection}", description: "The maximum number of idle open connections allowed.");
static readonly UpDownCounter<int> MinIdleConnectionsCounter = Meter.CreateUpDownCounter<int>("db.client.connections.idle.min",
  unit: "{connection}", description: "The minimum number of idle open connections allowed.");
static readonly UpDownCounter<int> MaxConnectionsCounter = Meter.CreateUpDownCounter<int>("db.client.connections.max",
  unit: "{connection}", description: "The maximum number of open connections allowed.");
static readonly UpDownCounter<int> PendingRequestsCounter = Meter.CreateUpDownCounter<int>("db.client.connections.pending_requests",
  unit: "{request}", description: "The number of pending requests for an open connection, cumulative for the entire pool.");
static readonly Counter<int> TimeoutsCounter = Meter.CreateCounter<int>("db.client.connections.timeouts",
  unit: "{timeout}", description: "The number of connection timeouts that have occurred trying to obtain a connection from the pool.");
static readonly Histogram<float> CreateTimeHistogram = Meter.CreateHistogram<float>("db.client.connections.create_time",
  unit: "ms", description: "The time it took to create a new connection.");
static readonly Histogram<float> WaitTimeHistogram = Meter.CreateHistogram<float>("db.client.connections.wait_time",
  unit: "ms", description: "The time it took to obtain an open connection from the pool.");
static readonly Histogram<float> UseTimeHistogram = Meter.CreateHistogram<float>("db.client.connections.use_time",
  unit: "ms", description: "The time it took to create a new connection.");
```

* _TODO: Use `float` as the unit for time measurement (in milliseconds), or `int` or `double`?_

ADO.NET Providers SHOULD support as many of these instruments as is possible.
Providers MUST use the names, units, and descriptions given in the example code above.

For the `create_time` and `wait_time` instruments, the reported time SHOULD be as close as possible to the entire duration
of the `DbConnection.Open(Async)` or `DbDataSource.OpenConnection(Async)` method,
i.e., from when user code starts opening a connection to when an open `DbConnection` is returned.

For the `use_time` instrument, the reported time SHOULD measure the duration of the time from when an open
`DbConnection` is returned from `DbConnection.Open(Async)` or `DbDataSource.OpenConnection(Async)` to when
user code closes the connection (and returns it to the pool) via `DbConnection.Dispose(Async)` or `DbConnection.Close(Async)`.

If the ADO.NET Provider supports “Minimum Pool Size” and “Maximum Pool Size” connection string options and has a
fixed pool size, then `db.client.connections.idle.min` should be set (once) to the value of “Minimum Pool Size”,
and `db.client.connections.idle.max` and `db.client.connections.max` SHOULD each be set (once) to the value of
“Maximum Pool Size”.
If the ADO.NET Provider has a connection pool whose limits change dynamically during the lifetime of the application,
then the values of these instruments MUST be updated when the pool size limits change.

## Reporting Metrics

Use the [`UpDownCounter<T>.Add`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.updowncounter-1.add)
or [`Histogram<T>.Record`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.histogram-1.record)
methods to report metrics for the instruments described above.

## Example

A highly simplified example of reporting metrics might look like this:

```csharp
public class XyzsqlConnection
{
  // Assume the existence of a connection pooling implementation.
  private XyzsqlConnectionPool _pool;

  public override async Task OpenAsync(CancellationToken cancellationToken)
  {
    // Start timing the operation.
    var stopwatch = Stopwatch.StartNew();

    // Metrics should be tagged with the pool name if one is available; otherwise, use the connection string.
    var poolNameTag = new KeyValuePair<string, object?>("pool.name", _pool.Name ?? ConnectionString);

    // Increment the pending requests counter.
    PendingRequestsCounter.Add(1, poolNameTag);

    // Try to get a pooled session, falling back to creating a new one.
    if (_pool.TryGetSession(cancellationToken, out var session))
    {
      // Session was available in the pool; mark it as no longer idle and record the wait time.
      ConnectionsUsageCounter.Add(-1, new[] { poolNameTag, IdleStateTag });
      WaitTimeHistogram.Record((float) stopwatch.ElapsedMilliseconds);
    }
    else
    {
      // No session was available in the pool; create a new one and record the time taken.
      session = await _pool.CreateSessionAsync(cancellationToken);
      CreateTimeHistogram.Record((float) stopwatch.ElapsedMilliseconds);
    }

    // Increment the number of used connections.
    ConnectionsUsageCounter.Add(1, new[] { poolNameTag, UsedStateTag });
    PendingRequestsCounter.Add(-1, poolNameTag);
  }

  private static readonly Meter Meter = new("Xyzsql", "1.0.0");

  private static readonly KeyValuePair<string, object?> IdleStateTag = new("state", "idle");
  private static readonly KeyValuePair<string, object?> UsedStateTag = new("state", "used");

  private static readonly UpDownCounter<int> ConnectionsUsageCounter = Meter.CreateUpDownCounter<int>("db.client.connections.usage",
    unit: "{connection}", description: "The number of connections that are currently in the state described by the state tag.");
  private static readonly UpDownCounter<int> PendingRequestsCounter = Meter.CreateUpDownCounter<int>("db.client.connections.pending_requests",
    unit: "{request}", description: "The number of pending requests for an open connection, cumulative for the entire pool.");
  private static readonly Histogram<float> CreateTimeHistogram = Meter.CreateHistogram<float>("db.client.connections.create_time",
    unit: "ms", description: "The time it took to create a new connection.");
  private static readonly Histogram<float> WaitTimeHistogram = Meter.CreateHistogram<float>("db.client.connections.wait_time",
    unit: "ms", description: "The time it took to obtain an open connection from the pool.");
}
```

For a more complex example, see the [proposed MySqlConnector changes on the `metrics` branch](https://github.com/mysql-net/MySqlConnector/compare/master...bgrainger:MySqlConnector:metrics).
