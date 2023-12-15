---
date: 2022-07-10
lastmod: 2023-12-15
title: MetaDataCollections Schema
---

# MetaDataCollections Schema

The `MetaDataCollections` schema provides information about available schemas.

Column Name | Data Type | Description
--- | --- | ---
CollectionName | string | The name of the collection to pass to the GetSchema method to return the collection.
NumberOfRestrictions | int | The number of restrictions that may be specified for the collection.
NumberOfIdentifierParts | int | The number of parts in the composite identifier/database object name.

