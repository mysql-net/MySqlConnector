---
lastmod: 2016-10-16
date: 2016-10-16
menu:
  main:
    parent: getting started
title: Installing
weight: 10
---

Installing
==========

The recommended way of installing MySqlConnector is through [NuGet](https://www.nuget.org/packages/MySqlConnector/)

**Step 1:** Add MySqlConnector to the dependencies in your `project.json` file:

```json
{
  "title": "My Application",
  "description": "A great application",
  "dependencies": {
    "MySqlConnector": "0.*",
    // other dependencies
  },
  // other config
}
  ```

**Step 2:** Run the command `dotnet restore`