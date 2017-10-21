namespace MySql.Data.MySqlClient.Types
{
	internal sealed class ColumnTypeMapping
	{
		public static string CreateLookupKey(string columnTypeName, bool isUnsigned, int length) => $"{columnTypeName}|{(isUnsigned ? "u" : "s")}|{length}";

		public ColumnTypeMapping(string dataTypeName, DbTypeMapping dbTypeMapping, MySqlDbType mySqlDbType, bool unsigned = false, bool binary = false, int length = 0, string simpleDataTypeName = null)
		{
			DataTypeName = dataTypeName;
			SimpleDataTypeName = simpleDataTypeName ?? dataTypeName;
			DbTypeMapping = dbTypeMapping;
			MySqlDbType = mySqlDbType;
			Unsigned = unsigned;
			Binary = binary;
			Length = length;
		}

		public string DataTypeName { get; }
		public string SimpleDataTypeName { get; }
		public DbTypeMapping DbTypeMapping { get; }
		public MySqlDbType MySqlDbType { get; }
		public bool Binary { get; }
		public bool Unsigned { get; }
		public int Length { get; }

		public string CreateLookupKey() => CreateLookupKey(DataTypeName, Unsigned, Length);
	}
}
