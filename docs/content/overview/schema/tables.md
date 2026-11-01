---
date: 2022-07-10
lastmod: 2026-11-01
title: Tables Schema
---

# Tables Schema

The `Tables` schema provides information about tables.

Column Name | Data Type | Description
--- | --- | ---
TABLE_CATALOG | string | 
TABLE_SCHEMA | string | 
TABLE_NAME | string | 
TABLE_TYPE | string | 
ENGINE | string | 
VERSION | string | 
ROW_FORMAT | string | 
TABLE_ROWS | ulong | 
AVG_ROW_LENGTH | ulong | 
DATA_LENGTH | ulong | 
MAX_DATA_LENGTH | ulong | 
INDEX_LENGTH | ulong | 
DATA_FREE | ulong | 
AUTO_INCREMENT | ulong | 
CREATE_TIME | DateTime | 
UPDATE_TIME | DateTime | 
CHECK_TIME | DateTime | 
TABLE_COLLATION | string | 
CHECKSUM | string | 
CREATE_OPTIONS | string | 
TABLE_COMMENT | string | 

The following restrictions are supported:

Restriction Name | Restriction Default | Restriction Number
--- | --- | --:
Catalog | TABLE_CATALOG | 1
Schema | TABLE_SCHEMA | 2
Table | TABLE_NAME | 3
TableType | TABLE_TYPE | 4

