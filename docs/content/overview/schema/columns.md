---
date: 2022-07-10
lastmod: 2023-12-14
title: Columns Schema
---

# Columns Schema

The `Columns` schema provides information about columns (in all tables).

Column Name | Data Type | Description
--- | --- | ---
TABLE_CATALOG | string | 
TABLE_SCHEMA | string | 
TABLE_NAME | string | 
COLUMN_NAME | string | 
ORDINAL_POSITION | uint | 
COLUMN_DEFAULT | string | 
IS_NULLABLE | string | 
DATA_TYPE | string | 
CHARACTER_MAXIMUM_LENGTH | long | 
NUMERIC_PRECISION | ulong | 
NUMERIC_SCALE | ulong | 
DATETIME_PRECISION | uint | 
CHARACTER_SET_NAME | string | 
COLLATION_NAME | string | 
COLUMN_TYPE | string | 
COLUMN_KEY | string | 
EXTRA | string | 
PRIVILEGES | string | 
COLUMN_COMMENT | string | 
GENERATION_EXPRESSION | string | 
SRS_ID | string | 

The following restrictions are supported:

Restriction Name | Restriction Default | Restriction Number
--- | --- | --:
Catalog | TABLE_CATALOG | 1
Schema | TABLE_SCHEMA | 2
Table | TABLE_NAME | 3
Column | COLUMN_NAME | 4

