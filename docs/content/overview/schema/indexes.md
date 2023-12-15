---
date: 2022-07-10
lastmod: 2023-12-15
title: Indexes Schema
---

# Indexes Schema

The `Indexes` schema provides information about indexes in the server's SQL syntax.

Column Name | Data Type | Description
--- | --- | ---
SEQ_IN_INDEX | long | 
INDEX_CATALOG | string | 
INDEX_SCHEMA | string | 
INDEX_NAME | string | 
TABLE_NAME | string | 
UNIQUE | bool | 
PRIMARY | bool | 
TYPE | string | 
COMMENT | string | 

The following restrictions are supported:

Restriction Name | Restriction Default | Restriction Number
--- | --- | --:
Catalog | TABLE_CATALOG | 1
Schema | TABLE_SCHEMA | 2
Table | TABLE_NAME | 3
Column | COLUMN_NAME | 4

