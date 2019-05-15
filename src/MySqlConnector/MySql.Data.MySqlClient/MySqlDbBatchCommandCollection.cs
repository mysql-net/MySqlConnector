using System.Collections.ObjectModel;
using System.Data.Common;

#if NET45 || NET461 || NET471 || NETSTANDARD1_3 || NETSTANDARD2_0 || NETCOREAPP2_1
namespace System.Data.Common
{
	public abstract class DbBatchCommandCollection : Collection<DbBatchCommand>
	{
	}
}
#endif

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlDbBatchCommandCollection : DbBatchCommandCollection
	{
		public new MySqlDbBatchCommand this[int index]
		{
			get => (MySqlDbBatchCommand) base[index];
			set => base[index] = value;
		}
	}
}
