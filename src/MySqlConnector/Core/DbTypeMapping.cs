using System.Globalization;

namespace MySqlConnector.Core;

internal sealed class DbTypeMapping(Type clrType, DbType[] dbTypes, Func<object, object>? convert = null)
{
	public Type ClrType { get; } = clrType;
	public DbType[] DbTypes { get; } = dbTypes;

	public object DoConversion(object obj)
	{
		if (obj.GetType() == ClrType)
			return obj;
		return convert is null ? Convert.ChangeType(obj, ClrType, CultureInfo.InvariantCulture)! : convert(obj);
	}
}
