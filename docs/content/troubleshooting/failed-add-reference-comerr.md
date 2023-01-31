---
lastmod: 2023-01-30
date: 2023-01-28
title: Failed to Add comerr
description: How to fix the error "Failed to add reference to 'comerr64'" when installing MySql.Data using NuGet.
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

## Fix

### Recommended Fix

The recommended fix for this problem is to [switch to MySqlConnector](/overview/installing), which fixes this problem and [many other bugs](/tutorials/migrating-from-connector-net/#fixed-bugs) in MySql.Data.
To do this, use **Tools** > **NuGet Package Manager** > **Manage NuGet Packages for Solution...** to uninstall MySql.Data and install MySqlConnector instead.

For more information, see our guide on [migrating from MySql.Data to MySqlConnector](/tutorials/migrating-from-connector-net/).

### Alternative Fix

Users report that the problem only occurs when using `packages.config`.
You may be able to fix the problem by switching to `<PackageReference>` elements.
To do this, right-click `packages.config` in Solution Explorer and select **Migrate packages.config to PackageReference**.
For more information see [Migrate from packages.config to PackageReference](https://learn.microsoft.com/en-us/nuget/consume-packages/migrate-packages-config-to-package-reference) at MSDN.

After doing this, the installation of MySql.Data 8.0.32 should succeed.
A full conversion to SDK-style projects should not be required.

### Workaround

A workaround is to downgrade to MySql.Data 8.0.31 or earlier.
