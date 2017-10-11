---
lastmod: 2017-11-06
date: 2016-10-16
menu:
  main:
    parent: getting started
title: Installing
weight: 10
---

Installing
==========

The recommended way of installing MySqlConnector is through [NuGet](https://www.nuget.org/packages/MySqlConnector/).

### Automatically

If using the new project system, run: `dotnet add package MySqlConnector`

Or, in Visual Studio, use the _NuGet Package Manager_ to browse for and install `MySqlConnector`.

### Manually

**Step 1:** Add MySqlConnector to the dependencies in your `csproj` file:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyTitle>My Application</AssemblyTitle>
    <Description>A great application</Description>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp2.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MySqlConnector" Version="0.30.0" />
  </ItemGroup>

</Project>
```

**Step 2:** Run the command `dotnet restore`
