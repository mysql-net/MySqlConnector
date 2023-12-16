---
date: 2022-07-10
lastmod: 2023-12-16
title: Foreign Keys Schema
---

# Foreign Keys Schema

The `Foreign Keys` schema provides information about foreign keys in the server's SQL syntax.

Column Name | Data Type | Description
--- | --- | ---
CONSTRAINT_CATALOG | string | 
CONSTRAINT_SCHEMA | string | 
CONSTRAINT_NAME | string | 
TABLE_CATALOG | string | 
TABLE_SCHEMA | string | 
TABLE_NAME | string | 
MATCH_OPTION | string | 
UPDATE_RULE | string | 
DELETE_RULE | string | 
REFERENCED_TABLE_CATALOG | string | 
REFERENCED_TABLE_SCHEMA | string | 
REFERENCED_TABLE_NAME | string | 

The following restrictions are supported:

Restriction Name | Restriction Default | Restriction Number
--- | --- | --:
Catalog | TABLE_CATALOG | 1
Schema | TABLE_SCHEMA | 2
Table | TABLE_NAME | 3
Constraint Name | CONSTRAINT_NAME | 4

