namespace MySqlConnector;

/// <summary>
/// Represents the result of a <see cref="MySqlBulkCopy"/> operation.
/// </summary>
public sealed class MySqlBulkCopyResult
{
	/// <summary>
	/// The warnings, if any. Users of <see cref="MySqlBulkCopy"/> should check that this collection is empty to avoid
	/// potential data loss from failed data type conversions.
	/// </summary>
	public IReadOnlyList<MySqlError> Warnings { get; }

	/// <summary>
	/// The number of rows that were inserted during the bulk copy operation.
	/// </summary>
	public int RowsInserted { get; }

	internal MySqlBulkCopyResult(IReadOnlyList<MySqlError> warnings, int rowsInserted)
	{
		Warnings = warnings;
		RowsInserted = rowsInserted;
	}
}
