---
lastmod: 2024-01-14
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
`db.client.connections.timeouts` | Counter | `{timeout}` | The number of connection timeouts that have occurred trying to obtain a connection from the pool.
`db.client.connections.create_time` | Histogram | `s` | The time it took to create a new connection.
`db.client.connections.use_time` | Histogram | `s` | The time between borrowing a connection and returning it to the pool.
`db.client.connections.wait_time` | Histogram | `s` | The time it took to obtain an open connection from the pool.
`db.client.connections.idle.max` | UpDownCounter | `{connection}` | The maximum number of idle open connections allowed; this corresponds to `MaximumPoolSize` in the connection string.
`db.client.connections.idle.min` | UpDownCounter | `{connection}` | The minimum number of idle open connections allowed; this corresponds to `MinimumPoolSize` in the connection string.
`db.client.connections.max` | UpDownCounter | `{connection}` | The maximum number of open connections allowed; this corresponds to `MaximumPoolSize` in the connection string.

## Supported Tags

Name | Description
--- | ---
`pool.name` | The name of the connection pool (see below).
`state` | `idle` or `used`; this tag is used with the `db.client.connections.usage` instrument.

## Connection Pool Name

A connection pool name can be specified by one of the following means:

### UseName

The `MySqlDataSourceBuilder.UseName` method can be used to specify a name for the connection pool:

```csharp
await using var dataSource = new MySqlDataSourceBuilder("...connection string...")
    .UseName("MyPoolName")
    .Build();
```

This can also be used with dependency injection:

```csharp
builder.Services.AddMySqlDataSource(builder.Configuration.GetConnectionString("Default"),
    x => x.UseName("MyPoolName"));
```

### Keyed Services

Use the `AddKeyedMySqlDataSource` method to register a `MySqlDataSource` as a [keyed service](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8/runtime#keyed-di-services).
If the service key is a string, it will automatically be used as the `MySqlDataSource` name.

```csharp
builder.Services.AddKeyedMySqlDataSource("MyPoolName",
    builder.Configuration.GetConnectionString("MyConnectionString"));
```

### Application Name

Finally, the connection pool name can be specified by setting the `Application Name` connection string option:

```csharp
await using var connection = new MySqlConnection("server=dbserver;...;Application Name=MyPoolName");
```

If `UseName` is used, it will override the `Application Name` connection string option.

### Default

For metrics, if no pool name is specified, the connection string (without a password) will be used as the pool name.
