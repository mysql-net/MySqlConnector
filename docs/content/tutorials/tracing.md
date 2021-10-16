---
date: 2021-10-16
menu:
  main:
    parent: tutorials
title: Tracing
weight: 12
---

# Tracing

MySqlConnector implements `ActivitySource` for tracing its operations. The `ActivitySource` name is `MySqlConnector`.

The available activity names and tags are documented in [issue 1036](https://github.com/mysql-net/MySqlConnector/issues/1036).

## OpenTelemetry

To export traces using OpenTelemetry, install the [OpenTelemetry NuGet package](https://www.nuget.org/packages/OpenTelemetry/) and add code similar to the following:

```csharp
using var openTelemetry = Sdk.CreateTracerProviderBuilder()
	.AddSource("MySqlConnector")
	// add a destination, for example:
	// .AddZipkinExporter(o => { o.Endpoint = new Uri(...); })
	// .AddJaegerExporter(o => { o.AgentHost = "..."; o.AgentPort = 6831; })
```
