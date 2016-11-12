using System.Data;
using System.Linq;
using MySql.Data.MySqlClient.Types;

namespace MySql.Data.MySqlClient.Caches
{
	internal class CachedParameter
	{
		public CachedParameter(int ordinalPosition, string mode, string name, string dataType, bool unsigned)
		{
			Position = ordinalPosition;
			if (Position == 0)
			{
				Direction = ParameterDirection.ReturnValue;
			}
			else
			{
				switch (mode.ToLowerInvariant())
				{
					case "in":
						Direction = ParameterDirection.Input;
						break;
					case "inout":
						Direction = ParameterDirection.InputOutput;
						break;
					case "out":
						Direction = ParameterDirection.Output;
						break;
				}
			}
			Name = name;
			DbType = TypeMapper.Mapper.GetDbTypeMapping(dataType, unsigned).DbTypes?.First() ?? DbType.Object;
		}

		internal readonly int Position;
		internal readonly ParameterDirection Direction;
		internal readonly string Name;
		internal readonly DbType DbType;
	}
}
