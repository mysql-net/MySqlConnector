using System.Collections;

namespace MySqlConnector
{
	/// <summary>
	/// <see cref="MySqlAttributeCollection"/> represents a collection of query attributes that can be added to a <see cref="MySqlCommand"/>.
	/// </summary>
	public sealed class MySqlAttributeCollection : IEnumerable<MySqlAttribute>
	{
		/// <summary>
		/// Returns the number of attributes in the collection.
		/// </summary>
		public int Count => m_attributes.Count;

		/// <summary>
		/// Adds a new <see cref="MySqlAttribute"/> to the collection.
		/// </summary>
		/// <param name="attribute">The attribute to add.</param>
		/// <remarks>The attribute name must not be empty, and must not already exist in the collection.</remarks>
		public void Add(MySqlAttribute attribute)
		{
			ArgumentNullException.ThrowIfNull(attribute);
			ArgumentException.ThrowIfNullOrEmpty(attribute.AttributeName);
			foreach (var existingAttribute in m_attributes)
			{
				if (existingAttribute.AttributeName == attribute.AttributeName)
					throw new ArgumentException($"An attribute with the name {attribute.AttributeName} already exists in the collection", nameof(attribute));
			}
			m_attributes.Add(attribute);
		}

		/// <summary>
		/// Sets the attribute with the specified name to the given value, overwriting it if it already exists.
		/// </summary>
		/// <param name="attributeName">The attribute name.</param>
		/// <param name="value">The attribute value.</param>
		public void SetAttribute(string attributeName, object? value)
		{
			ArgumentException.ThrowIfNullOrEmpty(attributeName);
			for (var i = 0; i < m_attributes.Count; i++)
			{
				if (m_attributes[i].AttributeName == attributeName)
				{
					m_attributes[i] = new MySqlAttribute(attributeName, value);
					return;
				}
			}
			m_attributes.Add(new MySqlAttribute(attributeName, value));
		}

		/// <summary>
		/// Gets the attribute at the specified index.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns>The <see cref="MySqlAttribute"/> at that index.</returns>
		public MySqlAttribute this[int index] => m_attributes[index];

		/// <summary>
		/// Clears the collection.
		/// </summary>
		public void Clear() => m_attributes.Clear();

		/// <summary>
		/// Returns an enumerator for the collection.
		/// </summary>
		public IEnumerator<MySqlAttribute> GetEnumerator() => m_attributes.GetEnumerator();

		/// <summary>
		/// Removes the specified attribute from the collection.
		/// </summary>
		/// <param name="attribute">The attribute to remove.</param>
		/// <returns><c>true</c> if that attribute was removed; otherwise, <c>false</c>.</returns>
		public bool Remove(MySqlAttribute attribute) => m_attributes.Remove(attribute);

		/// <summary>
		/// Returns an enumerator for the collection.
		/// </summary>
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		internal MySqlAttributeCollection() => m_attributes = [];

		private readonly List<MySqlAttribute> m_attributes;
	}
}
