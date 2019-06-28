using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using MySqlConnector.Core;

#if NET45 || NET461 || NET471 || NETSTANDARD1_3 || NETSTANDARD2_0 || NETCOREAPP2_1 || NETCOREAPP3_0
namespace System.Data.Common
{
	public abstract class DbBatchCommandCollection : Collection<DbBatchCommand>
	{
	}
}
#endif

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlBatchCommandCollection : DbBatchCommandCollection, IReadOnlyList<IMySqlCommand>
	{
		public new MySqlBatchCommand this[int index]
		{
			get => (MySqlBatchCommand) base[index];
			set => base[index] = value;
		}

		IMySqlCommand IReadOnlyList<IMySqlCommand>.this[int index] => (IMySqlCommand) this[index];

		IEnumerator<IMySqlCommand> IEnumerable<IMySqlCommand>.GetEnumerator()
		{
			foreach (MySqlBatchCommand command in this)
				yield return command;
		}
	}
}
