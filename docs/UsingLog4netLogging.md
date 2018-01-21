## How to Use

To integrate MySqlConnector with log4net, add the following line of code to your application startup routine (before any `MySqlConnector` objects have been used):

```
MySqlConnectorLogManager.Provider = new Log4netLoggerProvider();
```
