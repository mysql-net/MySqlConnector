---
title: MySqlConnector
---

## MySqlConnector namespace

| public type | description |
| --- | --- |
| class [MySqlBatch](../MySqlConnector/MySqlBatchType/) | [`MySqlBatch`](../MySqlConnector/MySqlBatchType/) implements the new [ADO.NET batching API](https://github.com/dotnet/runtime/issues/28633). It is currently experimental and may change in the future. |
| class [MySqlBatchCommand](../MySqlConnector/MySqlBatchCommandType/) |  |
| class [MySqlBatchCommandCollection](../MySqlConnector/MySqlBatchCommandCollectionType/) |  |
| class [MySqlBulkCopy](../MySqlConnector/MySqlBulkCopyType/) | [`MySqlBulkCopy`](../MySqlConnector/MySqlBulkCopyType/) lets you efficiently load a MySQL Server table with data from another source. It is similar to the [SqlBulkCopy](https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlbulkcopy) class for SQL Server. |
| class [MySqlBulkCopyColumnMapping](../MySqlConnector/MySqlBulkCopyColumnMappingType/) | Use [`MySqlBulkCopyColumnMapping`](../MySqlConnector/MySqlBulkCopyColumnMappingType/) to specify how to map columns in the source data to columns in the destination table when using [`MySqlBulkCopy`](../MySqlConnector/MySqlBulkCopyType/). |
| class [MySqlBulkLoader](../MySqlConnector/MySqlBulkLoaderType/) | [`MySqlBulkLoader`](../MySqlConnector/MySqlBulkLoaderType/) lets you efficiently load a MySQL Server Table with data from a CSV or TSV file or Stream. |
| enum [MySqlBulkLoaderConflictOption](../MySqlConnector/MySqlBulkLoaderConflictOptionType/) |  |
| enum [MySqlBulkLoaderPriority](../MySqlConnector/MySqlBulkLoaderPriorityType/) |  |
| enum [MySqlCertificateStoreLocation](../MySqlConnector/MySqlCertificateStoreLocationType/) |  |
| class [MySqlCommand](../MySqlConnector/MySqlCommandType/) | [`MySqlCommand`](../MySqlConnector/MySqlCommandType/) represents a SQL statement or stored procedure name to execute against a MySQL database. |
| class [MySqlCommandBuilder](../MySqlConnector/MySqlCommandBuilderType/) |  |
| class [MySqlConnection](../MySqlConnector/MySqlConnectionType/) | [`MySqlConnection`](../MySqlConnector/MySqlConnectionType/) represents a connection to a MySQL database. |
| enum [MySqlConnectionProtocol](../MySqlConnector/MySqlConnectionProtocolType/) | Specifies the type of connection to make to the server. |
| class [MySqlConnectionStringBuilder](../MySqlConnector/MySqlConnectionStringBuilderType/) |  |
| class [MySqlConnectorFactory](../MySqlConnector/MySqlConnectorFactoryType/) |  |
| class [MySqlConversionException](../MySqlConnector/MySqlConversionExceptionType/) |  |
| class [MySqlDataAdapter](../MySqlConnector/MySqlDataAdapterType/) |  |
| class [MySqlDataReader](../MySqlConnector/MySqlDataReaderType/) |  |
| struct [MySqlDateTime](../MySqlConnector/MySqlDateTimeType/) |  |
| enum [MySqlDateTimeKind](../MySqlConnector/MySqlDateTimeKindType/) | The DateTimeKind used when reading DateTime from the database. |
| class [MySqlDbColumn](../MySqlConnector/MySqlDbColumnType/) |  |
| enum [MySqlDbType](../MySqlConnector/MySqlDbTypeType/) |  |
| class [MySqlError](../MySqlConnector/MySqlErrorType/) |  |
| enum [MySqlErrorCode](../MySqlConnector/MySqlErrorCodeType/) | MySQL Server error codes. Taken from [Server Error Codes and Messages](https://dev.mysql.com/doc/refman/5.7/en/error-messages-server.html). |
| class [MySqlException](../MySqlConnector/MySqlExceptionType/) | [`MySqlException`](../MySqlConnector/MySqlExceptionType/) is thrown when MySQL Server returns an error code, or there is a communication error with the server. |
| class [MySqlGeometry](../MySqlConnector/MySqlGeometryType/) | Represents MySQL's internal GEOMETRY format: https://dev.mysql.com/doc/refman/8.0/en/gis-data-formats.html#gis-internal-format |
| enum [MySqlGuidFormat](../MySqlConnector/MySqlGuidFormatType/) | Determines which column type (if any) should be read as a `System.Guid`. |
| class [MySqlHelper](../MySqlConnector/MySqlHelperType/) |  |
| class [MySqlInfoMessageEventArgs](../MySqlConnector/MySqlInfoMessageEventArgsType/) |  |
| delegate [MySqlInfoMessageEventHandler](../MySqlConnector/MySqlInfoMessageEventHandlerType/) |  |
| enum [MySqlLoadBalance](../MySqlConnector/MySqlLoadBalanceType/) |  |
| class [MySqlParameter](../MySqlConnector/MySqlParameterType/) |  |
| class [MySqlParameterCollection](../MySqlConnector/MySqlParameterCollectionType/) |  |
| class [MySqlProtocolException](../MySqlConnector/MySqlProtocolExceptionType/) |  |
| class [MySqlRowsCopiedEventArgs](../MySqlConnector/MySqlRowsCopiedEventArgsType/) |  |
| delegate [MySqlRowsCopiedEventHandler](../MySqlConnector/MySqlRowsCopiedEventHandlerType/) | Represents the method that handles the [`MySqlRowsCopied`](../MySqlConnector/MySqlBulkCopy/MySqlRowsCopied/) event of a [`MySqlBulkCopy`](../MySqlConnector/MySqlBulkCopyType/). |
| class [MySqlRowUpdatedEventArgs](../MySqlConnector/MySqlRowUpdatedEventArgsType/) |  |
| delegate [MySqlRowUpdatedEventHandler](../MySqlConnector/MySqlRowUpdatedEventHandlerType/) |  |
| class [MySqlRowUpdatingEventArgs](../MySqlConnector/MySqlRowUpdatingEventArgsType/) |  |
| delegate [MySqlRowUpdatingEventHandler](../MySqlConnector/MySqlRowUpdatingEventHandlerType/) |  |
| enum [MySqlSslMode](../MySqlConnector/MySqlSslModeType/) | SSL connection options. |
| class [MySqlTransaction](../MySqlConnector/MySqlTransactionType/) |  |

<!-- DO NOT EDIT: generated by xmldocmd for MySqlConnector.dll -->
