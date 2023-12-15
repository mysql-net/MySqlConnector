---
date: 2022-07-10
lastmod: 2023-12-15
title: Restrictions Schema
---

# Restrictions Schema

The `Restrictions` schema provides information about the restrictions supported when retrieving schemas.

Column Name | Data Type | Description
--- | --- | ---
CollectionName | string | The name of the collection that these restrictions apply to.
RestrictionName | string | The name of the restriction in the collection.
RestrictionDefault | string | Ignored.
RestrictionNumber | int | The actual location in the collections restrictions that this particular restriction falls in.

