## How to Use

To integrate MySqlConnector with [Serilog](https://serilog.net/), add the following line of code to your application startup routine (before any `MySqlConnector` objects have been used):

```
MySqlConnectorLogManager.Provider = new SerilogLoggerProvider();
```
