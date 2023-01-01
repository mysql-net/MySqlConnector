#if NET462
#pragma warning disable CA1716 // Don't use reserved language keywords
namespace System.Data.Common;

public abstract class DbColumn
{
	public bool? AllowDBNull { get; protected set; }
	public string? BaseCatalogName { get; protected set; }
	public string? BaseColumnName { get; protected set; }
	public string? BaseSchemaName { get; protected set; }
	public string? BaseServerName { get; protected set; }
	public string? BaseTableName { get; protected set; }
	public string ColumnName { get; protected set; } = "";
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
	public string? UdtAssemblyQualifiedName { get; protected set; }
	public Type? DataType { get; protected set; }
	public string? DataTypeName { get; protected set; }
	public virtual object? this[string property] => null;
}
#endif
