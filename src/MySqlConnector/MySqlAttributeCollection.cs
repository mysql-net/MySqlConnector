using System.Collections;

namespace MySqlConnector
{
	public sealed class MySqlAttributeCollection : ICollection<MySqlAttribute>
	{
		public int Count => m_attributes.Count;
		public bool IsReadOnly => false;
		public void Add(MySqlAttribute item) => throw new NotImplementedException();
		public void Clear() => throw new NotImplementedException();
		public bool Contains(MySqlAttribute item) => throw new NotImplementedException();
		public void CopyTo(MySqlAttribute[] array, int arrayIndex) => throw new NotImplementedException();
		public IEnumerator<MySqlAttribute> GetEnumerator() => m_attributes.GetEnumerator();
		public bool Remove(MySqlAttribute item) => throw new NotImplementedException();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		internal MySqlAttributeCollection() => m_attributes = new();

		private readonly List<MySqlAttribute> m_attributes;
	}
}
