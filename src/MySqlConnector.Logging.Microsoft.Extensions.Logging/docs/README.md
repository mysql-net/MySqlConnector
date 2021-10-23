## About

This package integrates MySqlConnector logging with the Microsoft.Extensions.Logging framework.

## How to Use

Add the following line of code to your `Startup.Configure` method (before any `MySqlConnector` objects have been used):

```csharp
MySqlConnectorLogManager.Provider = new MicrosoftExtensionsLoggingLoggerProvider(loggerFactory);
```
