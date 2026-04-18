## How to Use

> [!WARNING]
> The following information is for MySqlConnector versions prior to 2.3.0.
> The global logging interface that is described is deprecated and shouldn't be used in new code.
> For current logging guidance, see [Logging in MySqlConnector](https://mysqlconnector.net/diagnostics/logging/).

To integrate MySqlConnector with [Serilog](https://serilog.net/), add the following line of code to your application startup routine (before any `MySqlConnector` objects have been used):

```csharp
MySqlConnectorLogManager.Provider = new SerilogLoggerProvider();
```
