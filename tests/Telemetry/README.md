# Telemetry Demo

This folder contains an end-to-end OpenTelemetry demo for MySqlConnector.

The setup script starts two containers:

1. MySQL Server (v9.7 or later) with `component_query_attributes` and `component_telemetry` installed.
2. The Aspire Dashboard with OTLP/HTTP exposed at `http://localhost:4318` and the UI at `http://localhost:18888`.

## Run

```powershell
.\tests\Telemetry\setup.ps1
.\tests\Telemetry\verify.ps1
```

`setup.ps1` enables the Aspire telemetry API in unsecured mode for local use.
`verify.ps1` reuses the running containers, runs `Telemetry.cs`, reads `TRACE_ID=...` from the sample output, and checks the corresponding trace from the Aspire Dashboard API for MySqlConnector spans with MySQL child spans.
After running `setup.ps1`, open the printed Aspire Dashboard login URL to inspect the trace manually in the dashboard.

To generate traces, run:

```powershell
dotnet .\tests\Telemetry\Telemetry.cs
```

## Cleanup

```powershell
.\tests\Telemetry\teardown.ps1
```
