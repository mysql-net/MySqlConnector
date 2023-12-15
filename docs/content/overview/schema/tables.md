---
date: 2022-07-10
lastmod: 2023-12-15
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
TABLE_ROWS | long | 
AVG_ROW_LENGTH | long | 
DATA_LENGTH | long | 
MAX_DATA_LENGTH | long | 
INDEX_LENGTH | long | 
DATA_FREE | long | 
AUTO_INCREMENT | long | 
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

