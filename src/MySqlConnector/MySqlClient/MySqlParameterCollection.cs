using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlParameterCollection : DbParameterCollection, IEnumerable<MySqlParameter>
	{
		internal MySqlParameterCollection()
		{
			m_parameters = new List<MySqlParameter>();
			m_nameToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}

		public MySqlParameter Add(string parameterName, DbType dbType)
		{
			MySqlParameter parameter = new MySqlParameter
			{
				ParameterName = parameterName,
				DbType = dbType,
			};
			AddParameter(parameter);
			return parameter;
		}

		public override int Add(object value)
		{
			AddParameter((MySqlParameter) value);
			return m_parameters.Count - 1;
		}

		public override void AddRange(Array values)
		{
			foreach (var obj in values)
				Add(obj);
		}

		public MySqlParameter AddWithValue(string parameterName, object value)
		{
			var parameter = new MySqlParameter
			{
				ParameterName = parameterName,
				Value = value
			};
			AddParameter(parameter);
			return parameter;
		}

		public override bool Contains(object value)
		{
			return m_parameters.Contains((MySqlParameter) value);
		}

		public override bool Contains(string value)
		{
			return IndexOf(value) != -1;
		}

		public override void CopyTo(Array array, int index)
		{
			throw new NotSupportedException();
		}

		public override void Clear()
		{
			m_parameters.Clear();
			m_nameToIndex.Clear();
		}

		public override IEnumerator GetEnumerator()
		{
			return m_parameters.GetEnumerator();
		}

		IEnumerator<MySqlParameter> IEnumerable<MySqlParameter>.GetEnumerator()
		{
			return m_parameters.GetEnumerator();
		}

		protected override DbParameter GetParameter(int index)
		{
			return m_parameters[index];
		}

		protected override DbParameter GetParameter(string parameterName)
		{
			var index = IndexOf(parameterName);
			if (index == -1)
				throw new ArgumentException("Parameter '{0}' not found in the collection".FormatInvariant(parameterName), nameof(parameterName));
			return m_parameters[index];
		}

		public override int IndexOf(object value)
		{
			return m_parameters.IndexOf((MySqlParameter) value);
		}

		public override int IndexOf(string parameterName)
		{
			var index = NormalizedIndexOf(parameterName);
			if (index == -1)
				return -1;
			return string.Equals(parameterName, m_parameters[index].ParameterName, StringComparison.OrdinalIgnoreCase) ? index : -1;
		}

		internal int NormalizedIndexOf(string parameterName)
		{
			if (parameterName == null)
				throw new ArgumentNullException(nameof(parameterName));
			return m_nameToIndex.TryGetValue(MySqlParameter.NormalizeParameterName(parameterName), out var index) ? index : -1;
		}

		public override void Insert(int index, object value)
		{
			m_parameters.Insert(index, (MySqlParameter) value);
		}

#if !NETSTANDARD1_3
		public override bool IsFixedSize => false;
		public override bool IsReadOnly => false;
		public override bool IsSynchronized => false;
#endif

		public override void Remove(object value)
		{
			RemoveAt(IndexOf(value));
		}

		public override void RemoveAt(int index)
		{
			var oldParameter = m_parameters[index];
			if (oldParameter.NormalizedParameterName != null)
				m_nameToIndex.Remove(oldParameter.NormalizedParameterName);
			m_parameters.RemoveAt(index);

			foreach (var pair in m_nameToIndex.ToList())
			{
				if (pair.Value > index)
					m_nameToIndex[pair.Key] = pair.Value - 1;
			}
		}

		public override void RemoveAt(string parameterName)
		{
			RemoveAt(IndexOf(parameterName));
		}

		protected override void SetParameter(int index, DbParameter value)
		{
			var newParameter = (MySqlParameter) value;
			var oldParameter = m_parameters[index];
			if (oldParameter.NormalizedParameterName != null)
				m_nameToIndex.Remove(oldParameter.NormalizedParameterName);
			m_parameters[index] = newParameter;
			if (newParameter.NormalizedParameterName != null)
				m_nameToIndex.Add(newParameter.NormalizedParameterName, index);
		}

		protected override void SetParameter(string parameterName, DbParameter value)
		{
			SetParameter(IndexOf(parameterName), value);
		}

		public override int Count => m_parameters.Count;

		public override object SyncRoot
		{
			get { throw new NotSupportedException(); }
		}

		public new MySqlParameter this[int index]
		{
			get { return m_parameters[index]; }
			set { SetParameter(index, value); }
		}

		// Finds the index of a parameter by name, regardless of whether 'parameterName' or the matching
		// MySqlParameter.ParameterName has a leading '?' or '@'.
		internal int FlexibleIndexOf(string parameterName)
		{
			if (parameterName == null)
				throw new ArgumentNullException(nameof(parameterName));
			return m_nameToIndex.TryGetValue(MySqlParameter.NormalizeParameterName(parameterName), out var index) ? index : -1;
		}

		private void AddParameter(MySqlParameter parameter)
		{
			m_parameters.Add(parameter);
			if (parameter.NormalizedParameterName != null)
				m_nameToIndex[parameter.NormalizedParameterName] = m_parameters.Count - 1;
		}

		readonly List<MySqlParameter> m_parameters;
		readonly Dictionary<string, int> m_nameToIndex;
	}

}
