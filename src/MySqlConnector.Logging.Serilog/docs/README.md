## About

This package integrates MySqlConnector logging with [Serilog](https://www.nuget.org/packages/Serilog/).

## How to Use

Add the following line of code to your application startup routine (before any `MySqlConnector` objects have been used):

```csharp
MySqlConnectorLogManager.Provider = new SerilogLoggerProvider();
```
