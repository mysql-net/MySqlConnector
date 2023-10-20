namespace MySqlConnector.Core;

internal sealed class ColumnTypeMetadata(string dataTypeName, DbTypeMapping dbTypeMapping, MySqlDbType mySqlDbType, bool isUnsigned = false, bool binary = false, int length = 0, string? simpleDataTypeName = null, string? createFormat = null, long columnSize = 0)
{
	public static string CreateLookupKey(string columnTypeName, bool isUnsigned, int length) => $"{columnTypeName}|{(isUnsigned ? "u" : "s")}|{length}";

	public string DataTypeName { get; } = dataTypeName;
	public string SimpleDataTypeName { get; } = simpleDataTypeName ?? dataTypeName;
	public string CreateFormat { get; } = createFormat ?? (dataTypeName + (isUnsigned ? " UNSIGNED" : ""));
	public DbTypeMapping DbTypeMapping { get; } = dbTypeMapping;
	public MySqlDbType MySqlDbType { get; } = mySqlDbType;
	public bool Binary { get; } = binary;
	public long ColumnSize { get; } = columnSize;
	public bool IsUnsigned { get; } = isUnsigned;
	public int Length { get; } = length;

	public string CreateLookupKey() => CreateLookupKey(DataTypeName, IsUnsigned, Length);
}
