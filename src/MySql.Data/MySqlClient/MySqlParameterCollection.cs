using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlParameterCollection : DbParameterCollection
	{
		internal MySqlParameterCollection()
		{
			m_parameters = new List<MySqlParameter>();
		}

		public MySqlParameter Add(string parameterName, DbType dbType)
		{
			MySqlParameter parameter = new MySqlParameter
			{
				ParameterName = parameterName,
				DbType = dbType,
			};
			m_parameters.Add(parameter);
			return parameter;
		}

		public override int Add(object value)
		{
			m_parameters.Add((MySqlParameter) value);
			return m_parameters.Count - 1;
		}

		public override void AddRange(Array values)
		{
			foreach (var obj in values)
				Add(obj);
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
		}

		public override IEnumerator GetEnumerator()
		{
			return m_parameters.GetEnumerator();
		}

		protected override DbParameter GetParameter(int index)
		{
			return m_parameters[index];
		}

		protected override DbParameter GetParameter(string parameterName)
		{
			return m_parameters[IndexOf(parameterName)];
		}

		public override int IndexOf(object value)
		{
			return m_parameters.IndexOf((MySqlParameter) value);
		}

		public override int IndexOf(string parameterName)
		{
			return m_parameters.FindIndex(x => x.ParameterNameMatches(parameterName));
		}

		public override void Insert(int index, object value)
		{
			m_parameters.Insert(index, (MySqlParameter) value);
		}

		public override void Remove(object value)
		{
			m_parameters.Remove((MySqlParameter) value);
		}

		public override void RemoveAt(int index)
		{
			m_parameters.RemoveAt(index);
		}

		public override void RemoveAt(string parameterName)
		{
			RemoveAt(IndexOf(parameterName));
		}

		protected override void SetParameter(int index, DbParameter value)
		{
			m_parameters[index] = (MySqlParameter) value;
		}

		protected override void SetParameter(string parameterName, DbParameter value)
		{
			SetParameter(IndexOf(parameterName), value);
		}

		public override int Count
		{
			get { return m_parameters.Count; }
		}

		public override object SyncRoot
		{
			get { throw new NotSupportedException(); }
		}

		public new MySqlParameter this[int index]
		{
			get { return m_parameters[index]; }
			set { m_parameters[index] = value; }
		}

		readonly List<MySqlParameter> m_parameters;
	}

}
