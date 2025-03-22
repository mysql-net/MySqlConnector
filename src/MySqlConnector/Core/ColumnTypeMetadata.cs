using System.Runtime.CompilerServices;

namespace MySqlConnector.Core;

internal sealed class ColumnTypeMetadata(string dataTypeName, DbTypeMapping dbTypeMapping, MySqlDbType mySqlDbType, bool isUnsigned = false, bool binary = false, int length = 0, string? simpleDataTypeName = null, string? createFormat = null, long columnSize = 0, MySqlGuidFormat guidFormat = MySqlGuidFormat.Default)
{
	public static string CreateLookupKey(string columnTypeName, bool isUnsigned, int length, MySqlGuidFormat guidFormat) =>
		$"{columnTypeName}|{(isUnsigned ? "u" : "s")}|{length}|{GetGuidFormatLookupKey(guidFormat)}";

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string GetGuidFormatLookupKey(MySqlGuidFormat guidFormat) =>
		guidFormat switch
		{
			MySqlGuidFormat.Char36 => "c36",
			MySqlGuidFormat.Char32 => "c32",
			MySqlGuidFormat.Binary16 or MySqlGuidFormat.TimeSwapBinary16 or MySqlGuidFormat.LittleEndianBinary16 => "b16",
			_ => "def",
		};

	public string DataTypeName { get; } = dataTypeName;
	public string SimpleDataTypeName { get; } = simpleDataTypeName ?? dataTypeName;
	public string CreateFormat { get; } = createFormat ?? (dataTypeName + (isUnsigned ? " UNSIGNED" : ""));
	public DbTypeMapping DbTypeMapping { get; } = dbTypeMapping;
	public MySqlDbType MySqlDbType { get; } = mySqlDbType;
	public bool Binary { get; } = binary;
	public long ColumnSize { get; } = columnSize;
	public bool IsUnsigned { get; } = isUnsigned;
	public int Length { get; } = length;
	public MySqlGuidFormat GuidFormat { get; } = guidFormat;

	public string CreateLookupKey() => CreateLookupKey(DataTypeName, IsUnsigned, Length, GuidFormat);
}
