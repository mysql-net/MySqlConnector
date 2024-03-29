---
title: MySqlTransaction.Rollback methods
---

# MySqlTransaction.Rollback method (1 of 2)

Rolls back the database transaction.

```csharp
public override void Rollback()
```

## See Also

* class [MySqlTransaction](../../MySqlTransactionType/)
* namespace [MySqlConnector](../../MySqlTransactionType/)
* assembly [MySqlConnector](../../../MySqlConnectorAssembly/)

---

# MySqlTransaction.Rollback method (2 of 2)

Rolls back the current transaction to the savepoint with the specified *savepointName* without aborting the transaction.

```csharp
public override void Rollback(string savepointName)
```

| parameter | description |
| --- | --- |
| savepointName | The savepoint name. |

## Remarks

The name must have been created with [`Save`](../Save/), but not released by calling [`Release`](../Release/).

The proposed ADO.NET API that this is based on is not finalized; this API may change in the future.

## See Also

* class [MySqlTransaction](../../MySqlTransactionType/)
* namespace [MySqlConnector](../../MySqlTransactionType/)
* assembly [MySqlConnector](../../../MySqlConnectorAssembly/)

<!-- DO NOT EDIT: generated by xmldocmd for MySqlConnector.dll -->
