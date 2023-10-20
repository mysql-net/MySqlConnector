using System.Collections;

namespace MySqlConnector;

public sealed class MySqlBatchCommandCollection
#if NET6_0_OR_GREATER
	: DbBatchCommandCollection
#else
	: IList<MySqlBatchCommand>, ICollection<MySqlBatchCommand>, IEnumerable<MySqlBatchCommand>, IEnumerable
#endif
{
	internal MySqlBatchCommandCollection() => m_commands = [];

#if NET6_0_OR_GREATER
	public new MySqlBatchCommand this[int index] { get => (MySqlBatchCommand) base[index]; set => base[index] = value; }
	public override int Count => m_commands.Count;
	public override bool IsReadOnly => false;
	public override void Add(DbBatchCommand item) => m_commands.Add((MySqlBatchCommand) item);
	public override void Clear() => m_commands.Clear();
	public override bool Contains(DbBatchCommand item) => m_commands.Contains((MySqlBatchCommand) item);
	public override void CopyTo(DbBatchCommand[] array, int arrayIndex) => throw new NotImplementedException();
	public override IEnumerator<DbBatchCommand> GetEnumerator()
	{
		foreach (var command in m_commands)
			yield return command;
	}
	public override int IndexOf(DbBatchCommand item) => m_commands.IndexOf((MySqlBatchCommand) item);
	public override void Insert(int index, DbBatchCommand item) => m_commands.Insert(index, (MySqlBatchCommand) item);
	public override bool Remove(DbBatchCommand item) => m_commands.Remove((MySqlBatchCommand) item);
	public override void RemoveAt(int index) => m_commands.RemoveAt(index);
	protected override DbBatchCommand GetBatchCommand(int index) => m_commands[index];
	protected override void SetBatchCommand(int index, DbBatchCommand batchCommand) => m_commands[index] = (MySqlBatchCommand) batchCommand;
#else
	public MySqlBatchCommand this[int index] { get => m_commands[index]; set => m_commands[index] = value; }
	public int Count => m_commands.Count;
	public bool IsReadOnly => false;
	public void Add(MySqlBatchCommand item) => m_commands.Add((MySqlBatchCommand) item);
	public void Clear() => m_commands.Clear();
	public bool Contains(MySqlBatchCommand item) => m_commands.Contains((MySqlBatchCommand) item);
	public void CopyTo(MySqlBatchCommand[] array, int arrayIndex) => throw new NotImplementedException();
	public IEnumerator<MySqlBatchCommand> GetEnumerator()
	{
		foreach (var command in m_commands)
			yield return command;
	}
	public int IndexOf(MySqlBatchCommand item) => m_commands.IndexOf((MySqlBatchCommand) item);
	public void Insert(int index, MySqlBatchCommand item) => m_commands.Insert(index, (MySqlBatchCommand) item);
	public bool Remove(MySqlBatchCommand item) => m_commands.Remove((MySqlBatchCommand) item);
	public void RemoveAt(int index) => m_commands.RemoveAt(index);
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
#endif

	internal IReadOnlyList<MySqlBatchCommand> Commands => m_commands;

	private readonly List<MySqlBatchCommand> m_commands;
}
