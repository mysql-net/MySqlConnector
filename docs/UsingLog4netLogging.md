## How to Use

To integrate MySqlConnector with log4net, add the following line of code to your application startup routine (before any `MySqlConnector` objects have been used):

```
MySqlConnectorLogManager.Provider = new Log4netLoggerProvider();
```

To reduce the verbosity of MySqlConnector logging, add the following element to your log4net config:

```
<log4net>
  ...
  <logger name="MySqlConnector">
    <level value="WARN" />
  </logger>
```
