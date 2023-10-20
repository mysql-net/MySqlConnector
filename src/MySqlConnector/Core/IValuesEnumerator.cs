namespace MySqlConnector.Core;

/// <summary>
/// <see cref="IValuesEnumerator"/> provides an abstraction over iterating through a sequence of
/// rows, where each row can fill an array of field values.
/// </summary>
internal interface IValuesEnumerator
{
	int FieldCount { get; }
	ValueTask<bool> MoveNextAsync();
	bool MoveNext();
	void GetValues(object[] values);
}

internal sealed class DbDataReaderValuesEnumerator(DbDataReader dataReader) : IValuesEnumerator
{
	public int FieldCount => dataReader.FieldCount;

	public ValueTask<bool> MoveNextAsync() => new(dataReader.ReadAsync());

	public bool MoveNext() => dataReader.Read();

	public void GetValues(object[] values) => dataReader.GetValues(values);
}

internal sealed class DataReaderValuesEnumerator(IDataReader dataReader) : IValuesEnumerator
{
	public static IValuesEnumerator Create(IDataReader dataReader) => dataReader is DbDataReader dbDataReader ? (IValuesEnumerator) new DbDataReaderValuesEnumerator(dbDataReader) : new DataReaderValuesEnumerator(dataReader);

	public int FieldCount => dataReader.FieldCount;

	public ValueTask<bool> MoveNextAsync() => new(MoveNext());

	public bool MoveNext() => dataReader.Read();

	public void GetValues(object[] values) => dataReader.GetValues(values);
}

internal sealed class DataRowsValuesEnumerator(IEnumerable<DataRow> dataRows, int columnCount) : IValuesEnumerator
{
	public static IValuesEnumerator Create(DataTable dataTable) => new DataRowsValuesEnumerator(dataTable.Rows.Cast<DataRow>().Where(static x => x is not null).Select(static x => x!), dataTable.Columns.Count);

	public int FieldCount { get; } = columnCount;

	public ValueTask<bool> MoveNextAsync() => new(MoveNext());

	public bool MoveNext()
	{
		if (m_dataRows.MoveNext())
			return true;
		m_dataRows.Dispose();
		return false;
	}

	public void GetValues(object[] values)
	{
		var row = m_dataRows.Current;
		for (var i = 0; i < FieldCount; i++)
			values[i] = row[i];
	}

	private readonly IEnumerator<DataRow> m_dataRows = dataRows.GetEnumerator();
}
