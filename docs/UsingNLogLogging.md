## How to Use

To integrate MySqlConnector with [NLog](http://nlog-project.org/), add the following line of code to your application startup routine (before any `MySqlConnector` objects have been used):

```
MySqlConnectorLogManager.Provider = new NLogLoggerProvider();
```
