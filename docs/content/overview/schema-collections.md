---
lastmod: 2023-01-28
date: 2021-04-24
title: Schema Collections
customtitle: "Supported Schema Collections"
description: Information about the schemas supported by MySqlConnection.GetSchema.
menu:
  main:
    parent: getting started
weight: 80
---

# Supported Schema Collections

`DbConnection.GetSchema` retrieves schema information about the database that is currently connected. For background, see MSDN on [GetSchema and Schema Collections](https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/getschema-and-schema-collections) and [Common Schema Collections](https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/common-schema-collections).

`MySqlConnection.GetSchema` supports the following schemas:

* `MetaDataCollections`—[information about available schemas](../schema/metadatacollections/)
* `CharacterSets`
* `Collations`
* `CollationCharacterSetApplicability`
* `Columns`—[information about columns (in all tables)](../schema/columns/)
* `Databases`
* `DataSourceInformation`
* `DataTypes`—[information about available data types](../schema/datatypes/)
* `Engines`
* `KeyColumnUsage`
* `KeyWords`
* `Parameters`
* `Partitions`
* `Plugins`
* `Procedures`—[information about stored procedures](../schema/procedures/)
* `ProcessList`
* `Profiling`
* `ReferentialConstraints`
* `ReservedWords`—[information about reserved words in the server's SQL syntax](../schema/reservedwords/)
* `ResourceGroups`
* `Restrictions`—[information about the restrictions supported when retrieving schemas](../schema/restrictions/)
* `SchemaPrivileges`
* `Tables`—[information about tables](../schema/tables/)
* `TableConstraints`
* `TablePrivileges`
* `TableSpaces`
* `Triggers`
* `UserPrivileges`
* `Views`
