---
date: 2023-01-28
title: Failed to Add comerr
customtitle: "Fix: Failed to add reference to 'comerr64' when installing MySql.Data"
weight: 50
menu:
  main:
    parent: troubleshooting
---

# Fix: Failed to add reference to 'comerr64' when installing MySql.Data

## Problem

When installing MySql.Data 8.0.32, you may receive the following error:

```
Install-Package : Failed to add reference to 'comerr64'.
  Please make sure that the file is accessible, and that it is a valid assembly or COM component.
At line:1 char:1
+ Install-Package MySql.Data
```

Or, when running `dotnet build`, you may receive the following warnings:

```
C:\Program Files\dotnet\sdk\7.0.100\Microsoft.Common.CurrentVersion.targets(2352,5):
warning MSB3246: Resolved file has a bad image, no metadata, or is otherwise inaccessible. System.BadImageFormatException: PE image does not have metadata. [C:\MySql.csproj]
warning MSB3246:    at System.Reflection.Metadata.MetadataReader.GetAssemblyName(String assemblyFile) [C:\MySql.csproj]
warning MSB3246:    at Microsoft.Build.Shared.AssemblyNameExtension.GetAssemblyNameEx(String path) [C:\MySql.csproj]
warning MSB3246:    at Microsoft.Build.Tasks.SystemState.GetAssemblyName(String path) [C:\MySql.csproj]
warning MSB3246:    at Microsoft.Build.Tasks.ReferenceTable.SetPrimaryAssemblyReferenceItem(ITaskItem referenceAssemblyName) [C:\MySql.csproj]
```

## Cause

This is [a bug](https://bugs.mysql.com/bug.php?id=109716) in the MySql.Data NuGet package introduced in version 8.0.32.
Workarounds include switching to an SDK-style project (`.csproj` with `<PackageReference>` instead of `packages.config`), or downgrading to an earlier version of MySql.Data.

## Fix

A better long-term fix is to switch to MySqlConnector, which fixes this problem and [many other bugs](https://mysqlconnector.net/tutorials/migrating-from-connector-net/#fixed-bugs) in MySql.Data.
To do this, uninstall the MySql.Data package and run `dotnet add package MySqlConnector` to replace it with MySqlConnector.

For more information, see our guide on [migrating from MySql.Data to MySqlConnector](https://mysqlconnector.net/tutorials/migrating-from-connector-net/).
