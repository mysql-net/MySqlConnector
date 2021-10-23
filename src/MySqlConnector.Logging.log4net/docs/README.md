## About

This package integrates MySqlConnector logging with [log4net](https://www.nuget.org/packages/log4net/).

## How to Use

Add the following line of code to your application startup routine (before any `MySqlConnector` objects have been used):

```csharp
MySqlConnectorLogManager.Provider = new Log4netLoggerProvider();
```

To reduce the verbosity of MySqlConnector logging, add the following element to your log4net config:

```xml
<log4net>
  ...
  <logger name="MySqlConnector">
    <level value="WARN" />
  </logger>
```
