namespace MySqlConnector;

/// <summary>
/// <para>Use <see cref="MySqlBulkCopyColumnMapping"/> to specify how to map columns in the source data to
/// columns in the destination table when using <see cref="MySqlBulkCopy"/>.</para>
/// <para>Set <see cref="SourceOrdinal"/> to the zero-based index of the source column to map. Set <see cref="DestinationColumn"/> to
/// either the name of a column in the destination table, or the name of a user-defined variable.
/// If a user-defined variable, you can use <see cref="Expression"/> to specify a MySQL expression that assigns
/// its value to destination column.</para>
/// <para>Source columns that don't have an entry in <see cref="MySqlBulkCopy.ColumnMappings"/> will be ignored
/// (unless the <see cref="MySqlBulkCopy.ColumnMappings"/> collection is empty, in which case all columns will be mapped
/// one-to-one).</para>
/// <para>MySqlConnector will transmit all binary data as hex, so any expression that operates on it
/// must decode it with the <c>UNHEX</c> function first. (This will be performed automatically if no
/// <see cref="Expression"/> is specified, but will be necessary to specify manually for more complex expressions.)</para>
/// <para>Example code:</para>
/// <code>
/// new MySqlBulkCopyColumnMapping
/// {
///     SourceOrdinal = 2,
///     DestinationColumn = "user_name",
/// },
/// new MySqlBulkCopyColumnMapping
/// {
///     SourceOrdinal = 0,
///     DestinationColumn = "@tmp",
///     Expression = "column_value = @tmp * 2",
/// },
/// </code>
/// </summary>
/// <param name="sourceOrdinal">The zero-based ordinal position of the source column.</param>
/// <param name="destinationColumn">The name of the destination column.</param>
/// <param name="expression">The optional expression to be used to set the destination column.</param>
public sealed class MySqlBulkCopyColumnMapping(int sourceOrdinal, string destinationColumn, string? expression = null)
{
	/// <summary>
	/// Initializes <see cref="MySqlBulkCopyColumnMapping"/> with the default values.
	/// </summary>
	public MySqlBulkCopyColumnMapping()
		: this(0, "", null)
	{
	}

	/// <summary>
	/// The zero-based ordinal position of the source column to map from.
	/// </summary>
	public int SourceOrdinal { get; set; } = sourceOrdinal;

	/// <summary>
	/// The name of the destination column to copy to. To use an expression, this should be the name of a unique user-defined variable.
	/// </summary>
	public string DestinationColumn { get; set; } = destinationColumn;

	/// <summary>
	/// An optional expression for setting a destination column. To use an expression, the <see cref="DestinationColumn"/> should
	/// be set to the name of a user-defined variable and this expression should set a column using that variable.
	/// </summary>
	/// <remarks>To populate a binary column, you must set <see cref="DestinationColumn"/> to a variable name, and <see cref="Expression"/> to an
	/// expression that uses <code>UNHEX</code> to set the column value, e.g., <code>`destColumn` = UNHEX(@variableName)</code>.</remarks>
	public string? Expression { get; set; } = expression;
}
