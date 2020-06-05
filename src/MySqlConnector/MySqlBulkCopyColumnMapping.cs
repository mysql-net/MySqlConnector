using System;

namespace MySqlConnector
{
	/// <summary>
	/// <see cref="MySqlBulkCopyColumnMapping"/> specifies how to map columns in the source data to
	/// destination columns when using <see cref="MySqlBulkCopy"/>.
	/// </summary>
	public sealed class MySqlBulkCopyColumnMapping
	{
		/// <summary>
		/// Initializes <see cref="MySqlBulkCopyColumnMapping"/> with the default values.
		/// </summary>
		public MySqlBulkCopyColumnMapping()
		{
			DestinationColumn = "";
		}

		/// <summary>
		/// Initializes <see cref="MySqlBulkCopyColumnMapping"/> to the specified values.
		/// </summary>
		/// <param name="sourceOrdinal">The ordinal position of the source column.</param>
		/// <param name="destinationColumn">The name of the destination column.</param>
		/// <param name="expression">The optional expression to be used to set the destination column.</param>
		public MySqlBulkCopyColumnMapping(int sourceOrdinal, string destinationColumn, string? expression = null)
		{
			SourceOrdinal = sourceOrdinal;
			DestinationColumn = destinationColumn ?? throw new ArgumentNullException(nameof(destinationColumn));
			Expression = expression;
		}

		/// <summary>
		/// The ordinal position of the source column to map from.
		/// </summary>
		public int SourceOrdinal { get; set; }

		/// <summary>
		/// The name of the destination column to copy to. To use an expression, this should be the name of a unique user-defined variable.
		/// </summary>
		public string DestinationColumn { get; set; }

		/// <summary>
		/// An optional expression for setting a destination column. To use an expression, the <see cref="DestinationColumn"/> should
		/// be set to the name of a user-defined variable and this expression should set a column using that variable.
		/// </summary>
		/// <remarks>To populate a binary column, you must set <see cref="DestinationColumn"/> to a variable name, and <see cref="Expression"/> to an
		/// expression that uses <code>UNHEX</code> to set the column value, e.g., <code>`destColumn` = UNHEX(@variableName)</code>.</remarks>
		public string? Expression { get; set; }
	}
}
