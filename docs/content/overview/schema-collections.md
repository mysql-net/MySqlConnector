---
date: 2021-04-24
lastmod: 2022-07-11
menu:
  main:
    parent: getting started
title: Schema Collections
customtitle: "Supported Schema Collections"
weight: 80
---

# Supported Schema Collections

`DbConnection.GetSchema` retrieves schema information about the database that is currently connected. For background, see MSDN on [GetSchema and Schema Collections](https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/getschema-and-schema-collections) and [Common Schema Collections](https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/common-schema-collections).

`MySqlConnection.GetSchema` supports the following schemas:

* `MetaDataCollections`—information about available schemas
* `CharacterSets`
* `Collations`
* `CollationCharacterSetApplicability`
* `Columns`
* `Databases`
* `DataSourceInformation`
* `DataTypes`—information about available data types
* `Engines`
* `KeyColumnUsage`
* `KeyWords`
* `Parameters`
* `Partitions`
* `Plugins`
* `Procedures`—information about stored procedures
* `ProcessList`
* `Profiling`
* `ReferentialConstraints`
* `ReservedWords`—information about reserved words in the server's SQL syntax
* `ResourceGroups`
* `SchemaPrivileges`
* `Tables`
* `TableConstraints`
* `TablePrivileges`
* `TableSpaces`
* `Triggers`
* `UserPrivileges`
* `Views`

The `GetSchema(string, string[])` overload that specifies restrictions is not supported.
