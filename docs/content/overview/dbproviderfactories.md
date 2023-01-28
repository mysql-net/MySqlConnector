---
lastmod: 2023-01-28
date: 2019-03-06
menu:
  main:
    parent: getting started
title: DbProviderFactories
customtitle: "Using DbProviderFactories"
description: How to install MySqlConnector into DbProviderFactories by using app.config or DbProviderFactories.RegisterFactory.
weight: 15
---

# Using DbProviderFactories

MySqlConnector can be registered with `DbProviderFactories` and obtained via `DbProviderFactories.GetFactory("MySqlConnector")`, or by
using the methods [described here](https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/obtaining-a-dbproviderfactory).

## .NET Framework

For .NET Framework applications, add the following section to your `app.config` or `web.config`:

```xml
<system.data>
  <DbProviderFactories>
     <add name="MySqlConnector"
        invariant="MySqlConnector"
        description="Async MySQL ADO.NET Connector"
        type="MySqlConnector.MySqlConnectorFactory, MySqlConnector, Culture=neutral, PublicKeyToken=d33d3e53aa5f8c92" />
  </DbProviderFactories>
</system.data>
```

## .NET Core

For .NET Core 2.1 or later, call `DbProviderFactories.RegisterFactory("MySqlConnector", MySqlConnectorFactory.Instance)` during application
startup. This will register MySqlConnector's `DbProviderFactory` implementation in the central `DbProviderFactories` registry.
