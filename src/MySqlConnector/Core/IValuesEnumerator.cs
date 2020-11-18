using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace MySqlConnector.Core
{
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

	internal sealed class DbDataReaderValuesEnumerator : IValuesEnumerator
	{
		public DbDataReaderValuesEnumerator(DbDataReader dataReader) => m_dataReader = dataReader;

		public int FieldCount => m_dataReader.FieldCount;

		public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(m_dataReader.ReadAsync());

		public bool MoveNext() => m_dataReader.Read();

		public void GetValues(object[] values) => m_dataReader.GetValues(values);

		readonly DbDataReader m_dataReader;
	}

	internal sealed class DataReaderValuesEnumerator : IValuesEnumerator
	{
		public static IValuesEnumerator Create(IDataReader dataReader) => dataReader is DbDataReader dbDataReader ? (IValuesEnumerator) new DbDataReaderValuesEnumerator(dbDataReader) : new DataReaderValuesEnumerator(dataReader);

		public DataReaderValuesEnumerator(IDataReader dataReader) => m_dataReader = dataReader;

		public int FieldCount => m_dataReader.FieldCount;

		public ValueTask<bool> MoveNextAsync() => new(MoveNext());

		public bool MoveNext() => m_dataReader.Read();

		public void GetValues(object[] values) => m_dataReader.GetValues(values);

		readonly IDataReader m_dataReader;
	}

#if !NETSTANDARD1_3
	internal sealed class DataRowsValuesEnumerator : IValuesEnumerator
	{
		public static IValuesEnumerator Create(DataTable dataTable) => new DataRowsValuesEnumerator(dataTable.Rows.Cast<DataRow>().Where(static x => x is not null).Select(static x => x!), dataTable.Columns.Count);

		public DataRowsValuesEnumerator(IEnumerable<DataRow> dataRows, int columnCount)
		{
			m_dataRows = dataRows.GetEnumerator();
			FieldCount = columnCount;
		}

		public int FieldCount { get; }

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

		readonly IEnumerator<DataRow> m_dataRows;
	}
#endif
}
