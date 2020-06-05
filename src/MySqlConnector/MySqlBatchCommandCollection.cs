using System.Collections.Generic;
using System.Collections.ObjectModel;
using MySqlConnector.Core;

namespace MySqlConnector
{
	public sealed class MySqlBatchCommandCollection : Collection<MySqlBatchCommand>, IReadOnlyList<IMySqlCommand>
	{
		public new MySqlBatchCommand this[int index]
		{
			get => base[index];
			set => base[index] = value;
		}

		IMySqlCommand IReadOnlyList<IMySqlCommand>.this[int index] => this[index];

		IEnumerator<IMySqlCommand> IEnumerable<IMySqlCommand>.GetEnumerator()
		{
			foreach (var command in this)
				yield return command;
		}
	}
}
