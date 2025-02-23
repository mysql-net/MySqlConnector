namespace MySqlConnector;

#pragma warning disable CA1720 // Identifier contains type name

public enum MySqlDbType
{
	Bool = -1,
	Decimal,
	Byte,
	Int16,
	Int32,
	Float,
	Double,
	Null,
	Timestamp,
	Int64,
	Int24,
	Date,
	Time,
	DateTime,
#pragma warning disable CA1069 // Enum values should not be duplicated
	[Obsolete("The Datetime enum value is obsolete.  Please use DateTime.")]
	Datetime = 12,
#pragma warning restore CA1069
	Year,
	Newdate,
	VarString,
	Bit,
	JSON = 245,
	NewDecimal,
	Enum,
	Set,
	TinyBlob,
	MediumBlob,
	LongBlob,
	Blob,
	VarChar,
	String,
	Geometry,
	Vector = 242,
	UByte = 501,
	UInt16,
	UInt32,
	UInt64 = 508,
	UInt24,
	Binary = 600,
	VarBinary,
	TinyText = 749,
	MediumText,
	LongText,
	Text,
	Guid = 800,
}
