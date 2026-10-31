namespace MySqlConnector.ColumnReaders;

/// <summary>
/// Specifies the kind of value a <see cref="ColumnReader"/> produces, and thus which of its strongly-typed <c>Read</c> methods can be called.
/// </summary>
internal enum ColumnReaderKind
{
	Other,
	Boolean,
	SignedInteger, // ReadInt64
	UnsignedInteger, // ReadUInt64
	Decimal,
	Double,
	Single,
	DateTime, // ReadDateTime and ReadMySqlDateTime
	TimeSpan,
	Guid,
	String, // ReadValue returns string
	Bytes, // ReadValue returns byte[]
}
