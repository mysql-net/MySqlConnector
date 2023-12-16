---
date: 2022-07-10
lastmod: 2023-12-16
title: IndexColumns Schema
---

# IndexColumns Schema

The `IndexColumns` schema provides information about indexes in the server's SQL syntax.

Column Name | Data Type | Description
--- | --- | ---
INDEX_CATALOG | string | 
INDEX_SCHEMA | string | 
INDEX_NAME | string | 
TABLE_NAME | string | 
COLUMN_NAME | string | 
ORDINAL_POSITION | int | 
SORT_ORDER | string | 

The following restrictions are supported:

Restriction Name | Restriction Default | Restriction Number
--- | --- | --:
Catalog | TABLE_CATALOG | 1
Schema | TABLE_SCHEMA | 2
Table | TABLE_NAME | 3
Name | INDEX_NAME | 4

