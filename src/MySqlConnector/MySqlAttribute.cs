namespace MySqlConnector
{
	/// <summary>
	/// <see cref="MySqlAttribute"/> represents an attribute that can be sent with a MySQL query.
	/// </summary>
	/// <remarks>See <a href="https://dev.mysql.com/doc/refman/8.0/en/query-attributes.html">Query Attributes</a> for information on using query attributes.</remarks>
	public sealed class MySqlAttribute
	{
		/// <summary>
		/// Initializes a new <see cref="MySqlAttribute"/>.
		/// </summary>
		public MySqlAttribute()
		{
			AttributeName = "";
		}

		/// <summary>
		/// Initializes a new <see cref="MySqlAttribute"/> with the specified attribute name and value.
		/// </summary>
		public MySqlAttribute(string attributeName, object? value)
		{
			AttributeName = attributeName ?? "";
			Value = value;
		}

		/// <summary>
		/// Gets or sets the attribute name.
		/// </summary>
		public string AttributeName { get; set; }

		/// <summary>
		/// Gets or sets the attribute value.
		/// </summary>
		public object? Value { get; set; }

		internal MySqlParameter ToParameter()
		{
			if (string.IsNullOrEmpty(AttributeName))
				throw new InvalidOperationException("AttributeName must not be null or empty");
			return new(AttributeName, Value);
		}
	}
}
