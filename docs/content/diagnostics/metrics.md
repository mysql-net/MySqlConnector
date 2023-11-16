---
date: 2023-11-11
menu:
  main:
    parent: diagnostics
title: Metrics
weight: 30
---

# Metrics

MySqlConnector exposes metrics that follow the [OpenTelemetry Semantic Conventions for Database Metrics](https://opentelemetry.io/docs/specs/semconv/database/database-metrics/) via types in the [System.Diagnostics.Metrics](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics) namespace.

<blockquote class="note">Metrics are only supported in MySqlConnector 2.3.0 or later.</blockquote>

The name used for the meter is `MySqlConnector`.

Connect a listener using the `MeterListener` class:

```csharp
new MeterListener
{
    InstrumentPublished = (instrument, listener) =>
    {
        if (instrument.Meter.Name == "MySqlConnector")
            listener.EnableMeasurementEvents(instrument);
    }
};
```

## Supported Instruments

Name | Type | Unit | Description
--- | --- | --- | ---
`db.client.connections.usage` | UpDownCounter | `{connection}` | The number of connections that are currently in the state described by the state tag.
`db.client.connections.pending_requests` | UpDownCounter | `{request}` | The number of pending requests for an open connection, cumulative for the entire pool.
`db.client.connections.create_time` | Histogram | `s` | The time it took to create a new connection.
`db.client.connections.use_time` | Histogram | `s` | The time between borrowing a connection and returning it to the pool.
`db.client.connections.wait_time` | Histogram | `s` | The time it took to obtain an open connection from the pool.
`db.client.connections.idle.max` | UpDownCounter | `{connection}` | The maximum number of idle open connections allowed; this corresponds to `MaximumPoolSize` in the connection string.
`db.client.connections.idle.min` | UpDownCounter | `{connection}` | The minimum number of idle open connections allowed; this corresponds to `MinimumPoolSize` in the connection string.
`db.client.connections.max` | UpDownCounter | `{connection}` | The maximum number of open connections allowed; this corresponds to `MaximumPoolSize` in the connection string.
