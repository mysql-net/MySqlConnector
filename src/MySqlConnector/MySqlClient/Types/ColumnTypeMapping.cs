using System.Collections.Generic;
using MySql.Data.Serialization;

namespace MySql.Data.MySqlClient.Types
{
	internal class ColumnTypeMapping
	{
		internal static string CreateLookupKey(string columnTypeName, bool unsigned, int length)
		{
			return columnTypeName.Trim().ToLowerInvariant()
			       + "|" + (unsigned ? "u" : "s")
			       + "|" + length;
		}

		public ColumnTypeMapping(string columnTypeName, DbTypeMapping dbTypeMapping, IEnumerable<ColumnType> columnTypes,
			bool unsigned=false,
			bool binary=false,
			int length=0
		)
		{
			ColumnTypeName = columnTypeName;
			DbTypeMapping = dbTypeMapping;
			Unsigned = unsigned;
			Binary = binary;
			Length = length;
		}

		internal readonly string ColumnTypeName;
		internal readonly DbTypeMapping DbTypeMapping;
		internal readonly IEnumerable<ColumnType> ColumnTypes;
		internal readonly bool Binary;
		internal readonly bool Unsigned;
		internal readonly int Length;

		internal string LookupKey => CreateLookupKey(ColumnTypeName, Unsigned, Length);
	}
}