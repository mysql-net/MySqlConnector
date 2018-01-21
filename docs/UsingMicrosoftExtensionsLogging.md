## How to Use

To integrate MySqlConnector with Microsoft.Extensions.Logging, add the following line of code to your `Startup.Configure` method (before any `MySqlConnector` objects have been used):

```
MySqlConnectorLogManager.Provider = new MicrosoftExtensionsLoggingLoggerProvider(loggerFactory);
```
