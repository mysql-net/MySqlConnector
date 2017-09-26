using System;
using System.Data.Common;
using MySql.Data.Serialization;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlDbColumn
#if NETSTANDARD1_3 || NETSTANDARD2_0
		: DbColumn
#endif
	{
		internal MySqlDbColumn(int ordinal, ColumnDefinitionPayload column, Type type, string dataTypeName)
		{
			var columnSize = type == typeof(string) || type == typeof(Guid) ?
				column.ColumnLength / SerializationUtility.GetBytesPerCharacter(column.CharacterSet) :
				column.ColumnLength;

			AllowDBNull = (column.ColumnFlags & ColumnFlags.NotNull) == 0;
			BaseCatalogName = null;
			BaseColumnName = column.PhysicalName;
			BaseSchemaName = column.SchemaName;
			BaseTableName = column.PhysicalTable;
			ColumnName = column.Name;
			ColumnOrdinal = ordinal;
			ColumnSize = columnSize > int.MaxValue ? int.MaxValue : unchecked((int) columnSize);
			DataType = type;
			DataTypeName = dataTypeName;
			IsAliased = column.PhysicalName != column.Name;
			IsAutoIncrement = (column.ColumnFlags & ColumnFlags.AutoIncrement) != 0;
			IsExpression = false;
			IsHidden = false;
			IsKey = (column.ColumnFlags & ColumnFlags.PrimaryKey) != 0;
			IsLong = column.ColumnLength > 255 &&
				((column.ColumnFlags & ColumnFlags.Blob) != 0 || column.ColumnType == ColumnType.TinyBlob || column.ColumnType == ColumnType.Blob || column.ColumnType == ColumnType.MediumBlob || column.ColumnType == ColumnType.LongBlob);
			IsReadOnly = false;
			IsUnique = (column.ColumnFlags & ColumnFlags.UniqueKey) != 0;
			if (column.ColumnType == ColumnType.Decimal || column.ColumnType == ColumnType.NewDecimal)
				NumericPrecision = (int) (column.ColumnLength - 2 + ((column.ColumnFlags & ColumnFlags.Unsigned) != 0 ? 1 : 0));
			NumericScale = column.Decimals;
			ProviderType = (int) column.ColumnType;
		}

		public int ProviderType { get; }

#if !NETSTANDARD1_3 && !NETSTANDARD2_0
		public bool? AllowDBNull { get; }
		public string BaseCatalogName { get; }
		public string BaseColumnName { get; }
		public string BaseSchemaName { get; }
		public string BaseTableName { get; }
		public string ColumnName { get; }
		public int? ColumnOrdinal { get; }
		public int? ColumnSize { get; }
		public Type DataType { get; }
		public string DataTypeName { get; }
		public bool? IsAliased { get; }
		public bool? IsAutoIncrement { get; }
		public bool? IsExpression { get; }
		public bool? IsHidden { get; }
		public bool? IsIdentity { get; }
		public bool? IsKey { get; }
		public bool? IsLong { get; }
		public bool? IsReadOnly { get; }
		public bool? IsUnique { get; }
		public int? NumericPrecision { get; }
		public int? NumericScale { get; }
		public string UdtAssemblyQualifiedName { get; }
#endif
	}
}
