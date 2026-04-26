# Telemetry Demo

This folder contains an end-to-end OpenTelemetry demo for MySqlConnector.

The setup script starts two containers:

1. MySQL Server with `component_query_attributes` and `component_telemetry` installed.
2. The Aspire Dashboard with OTLP/HTTP exposed at `http://localhost:4318` and the UI at `http://localhost:18888`.

## Important Version Note

MySQL Server 5.7 cannot satisfy this demo because it does not provide the query-attributes component or the OpenTelemetry server component needed for server-side spans. The demo uses `mysql:9.7`, which matches the Docker guidance in `tests/README.md` and the MySQL telemetry installation docs.

## Run

```powershell
.\tests\Telemetry\setup.ps1
.\tests\Telemetry\verify.ps1
```

`setup.ps1` enables the Aspire telemetry API in unsecured mode for local use. `verify.ps1` reuses the running containers, runs `Telemetry.cs`, reads `TRACE_ID=...` from the sample output, and checks the corresponding trace from the Aspire Dashboard API for MySqlConnector spans with MySQL child spans.

To inspect the trace manually in the dashboard, run:

```powershell
dotnet .\tests\Telemetry\Telemetry.cs
```

After running `setup.ps1`, open the printed Aspire Dashboard login URL to inspect the trace.

The sample app is a .NET 10 file-based app with `#:` directives, not a `.csproj`-based project. It issues one text command and one prepared command. Each query also reads `mysql_query_attribute_string('traceparent')` so the command output shows the exact W3C `traceparent` value seen by the server.

## Cleanup

```powershell
.\tests\Telemetry\teardown.ps1
```
