using System;
using System.Collections.Generic;
using System.Data;

namespace MySql.Data.MySqlClient.Types
{
	internal class DbTypeMapping
	{
		public DbTypeMapping(Type clrType, IEnumerable<DbType> dbTypes,
			Func<object, object> convert = null)
		{
			ClrType = clrType;
			DbTypes = dbTypes;
			m_convert = convert;
		}

		internal readonly Type ClrType;
		internal readonly IEnumerable<DbType> DbTypes;

		internal object DoConversion(object obj)
		{
			if (obj.GetType() == ClrType)
				return obj;
			return m_convert == null ? Convert.ChangeType(obj, ClrType) : m_convert(obj);
		}

		readonly Func<object, object> m_convert;
	}
}