using System.Collections;

namespace MySqlConnector;

#pragma warning disable CA1010 // Generic interface should also be implemented

public sealed class MySqlParameterCollection : DbParameterCollection, IEnumerable<MySqlParameter>
{
	internal MySqlParameterCollection()
	{
		m_parameters = [];
		m_nameToIndex = new(StringComparer.OrdinalIgnoreCase);
	}

	public MySqlParameter Add(string parameterName, DbType dbType)
	{
		var parameter = new MySqlParameter
		{
			ParameterName = parameterName,
			DbType = dbType,
		};
		AddParameter(parameter, m_parameters.Count);
		return parameter;
	}

	public override int Add(object value)
	{
		ArgumentNullException.ThrowIfNull(value);
		AddParameter((MySqlParameter) value, m_parameters.Count);
		return m_parameters.Count - 1;
	}

	public MySqlParameter Add(MySqlParameter parameter)
	{
		ArgumentNullException.ThrowIfNull(parameter);
		AddParameter(parameter, m_parameters.Count);
		return parameter;
	}

	public MySqlParameter Add(string parameterName, MySqlDbType mySqlDbType) => Add(new(parameterName, mySqlDbType));
	public MySqlParameter Add(string parameterName, MySqlDbType mySqlDbType, int size) => Add(new(parameterName, mySqlDbType, size));

	public override void AddRange(Array values)
	{
		foreach (var obj in values)
			Add(obj!);
	}

	public MySqlParameter AddWithValue(string parameterName, object? value)
	{
		var parameter = new MySqlParameter
		{
			ParameterName = parameterName,
			Value = value,
		};
		AddParameter(parameter, m_parameters.Count);
		return parameter;
	}

	public override bool Contains(object value) => value is MySqlParameter parameter && m_parameters.Contains(parameter);

	public override bool Contains(string value) => IndexOf(value) != -1;

	public override void CopyTo(Array array, int index) => ((ICollection) m_parameters).CopyTo(array, index);

	public override void Clear()
	{
		foreach (var parameter in m_parameters)
			parameter.ParameterCollection = null;
		m_parameters.Clear();
		m_nameToIndex.Clear();
	}

	public override IEnumerator GetEnumerator() => m_parameters.GetEnumerator();

	IEnumerator<MySqlParameter> IEnumerable<MySqlParameter>.GetEnumerator() => m_parameters.GetEnumerator();

	protected override DbParameter GetParameter(int index) => m_parameters[index];

	protected override DbParameter GetParameter(string parameterName)
	{
		var index = IndexOf(parameterName);
		if (index == -1)
			throw new ArgumentException($"Parameter '{parameterName}' not found in the collection", nameof(parameterName));
		return m_parameters[index];
	}

	public override int IndexOf(object value) => value is MySqlParameter parameter ? m_parameters.IndexOf(parameter) : -1;

	public override int IndexOf(string parameterName) => NormalizedIndexOf(parameterName);

	// Finds the index of a parameter by name, regardless of whether 'parameterName' or the matching
	// MySqlParameter.ParameterName has a leading '?' or '@'.
	internal int NormalizedIndexOf(string? parameterName) =>
		UnsafeIndexOf(MySqlParameter.NormalizeParameterName(parameterName ?? ""));

	// Finds the index of a parameter by normalized name (i.e., the results of MySqlParameter.NormalizeParameterName).
	internal int UnsafeIndexOf(string? normalizedParameterName) =>
		m_nameToIndex.TryGetValue(normalizedParameterName ?? "", out var index) ? index : -1;

	public override void Insert(int index, object value) => AddParameter((MySqlParameter) (value ?? throw new ArgumentNullException(nameof(value))), index);

	public override bool IsFixedSize => false;
	public override bool IsReadOnly => false;
	public override bool IsSynchronized => false;

	public override void Remove(object value) => RemoveAt(IndexOf(value ?? throw new ArgumentNullException(nameof(value))));

	public override void RemoveAt(int index)
	{
		var oldParameter = m_parameters[index];
		if (oldParameter.NormalizedParameterName is not null)
			m_nameToIndex.Remove(oldParameter.NormalizedParameterName);
		oldParameter.ParameterCollection = null;
		m_parameters.RemoveAt(index);

		foreach (var pair in m_nameToIndex.ToList())
		{
			if (pair.Value > index)
				m_nameToIndex[pair.Key] = pair.Value - 1;
		}
	}

	public override void RemoveAt(string parameterName) => RemoveAt(IndexOf(parameterName));

	protected override void SetParameter(int index, DbParameter value)
	{
		ArgumentNullException.ThrowIfNull(value);
		var newParameter = (MySqlParameter) value;
		var oldParameter = m_parameters[index];
		if (oldParameter.NormalizedParameterName is not null)
			m_nameToIndex.Remove(oldParameter.NormalizedParameterName);
		oldParameter.ParameterCollection = null;
		m_parameters[index] = newParameter;
		if (newParameter.NormalizedParameterName is not null)
			m_nameToIndex.Add(newParameter.NormalizedParameterName, index);
		newParameter.ParameterCollection = this;
	}

	protected override void SetParameter(string parameterName, DbParameter value) => SetParameter(IndexOf(parameterName), value);

	public override int Count => m_parameters.Count;

	public override object SyncRoot => throw new NotSupportedException();

	public new MySqlParameter this[int index]
	{
		get => m_parameters[index];
		set => SetParameter(index, value);
	}

	public new MySqlParameter this[string name]
	{
		get => (MySqlParameter) GetParameter(name);
		set => SetParameter(name, value);
	}

	internal void ChangeParameterName(MySqlParameter parameter, string oldName, string newName)
	{
		if (m_nameToIndex.TryGetValue(oldName, out var index) && m_parameters[index] == parameter)
			m_nameToIndex.Remove(oldName);
		else
			index = m_parameters.IndexOf(parameter);

		if (newName.Length != 0)
		{
			if (m_nameToIndex.ContainsKey(newName))
				throw new MySqlException($"There is already a parameter with the name '{parameter.ParameterName}' in this collection.");
			m_nameToIndex[newName] = index;
		}
	}

	private void AddParameter(MySqlParameter parameter, int index)
	{
		if (!string.IsNullOrEmpty(parameter.NormalizedParameterName) && NormalizedIndexOf(parameter.NormalizedParameterName) != -1)
			throw new MySqlException($"Parameter '{parameter.ParameterName}' has already been defined.");
		if (index < m_parameters.Count)
		{
			foreach (var pair in m_nameToIndex.ToList())
			{
				if (pair.Value >= index)
					m_nameToIndex[pair.Key] = pair.Value + 1;
			}
		}
		m_parameters.Insert(index, parameter);
		if (!string.IsNullOrEmpty(parameter.NormalizedParameterName))
			m_nameToIndex[parameter.NormalizedParameterName] = index;
		parameter.ParameterCollection = this;
	}

	private readonly List<MySqlParameter> m_parameters;
	private readonly Dictionary<string, int> m_nameToIndex;
}
