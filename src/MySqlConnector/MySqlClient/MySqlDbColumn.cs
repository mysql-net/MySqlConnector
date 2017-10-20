using System;
using MySql.Data.Serialization;

#if !NETSTANDARD1_3 && !NETSTANDARD2_0
namespace System.Data.Common
{
	public abstract class DbColumn
	{
		public bool? AllowDBNull { get; protected set; }
		public string BaseCatalogName { get; protected set; }
		public string BaseColumnName { get; protected set; }
		public string BaseSchemaName { get; protected set; }
		public string BaseServerName { get; protected set; }
		public string BaseTableName { get; protected set; }
		public string ColumnName { get; protected set; }
		public int? ColumnOrdinal { get; protected set; }
		public int? ColumnSize { get; protected set; }
		public bool? IsAliased { get; protected set; }
		public bool? IsAutoIncrement { get; protected set; }
		public bool? IsExpression { get; protected set; }
		public bool? IsHidden { get; protected set; }
		public bool? IsIdentity { get; protected set; }
		public bool? IsKey { get; protected set; }
		public bool? IsLong { get; protected set; }
		public bool? IsReadOnly { get; protected set; }
		public bool? IsUnique { get; protected set; }
		public int? NumericPrecision { get; protected set; }
		public int? NumericScale { get; protected set; }
		public string UdtAssemblyQualifiedName { get; protected set; }
		public Type DataType { get; protected set; }
		public string DataTypeName { get; protected set; }
		public virtual object this[string property] => null;
	}
}
#endif

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlDbColumn : System.Data.Common.DbColumn
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
			{
				NumericPrecision = (int) column.ColumnLength;
				if ((column.ColumnFlags & ColumnFlags.Unsigned) == 0)
					NumericPrecision--;
				if (column.Decimals > 0)
					NumericPrecision--;
			}
			NumericScale = column.Decimals;
			ProviderType = (int) column.ColumnType;
		}

		public int ProviderType { get; }
	}
}
