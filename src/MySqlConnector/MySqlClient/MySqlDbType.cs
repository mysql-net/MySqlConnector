using System;

namespace MySql.Data.MySqlClient
{
	public enum MySqlDbType
	{
		Decimal,
		Byte,
		Int16,
		Int24 = 9,
		Int32 = 3,
		Int64 = 8,
		Float = 4,
		Double,
		Timestamp = 7,
		Date = 10,
		Time,
		DateTime,
		[Obsolete("The Datetime enum value is obsolete.  Please use DateTime.")]
		Datetime = 12,
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
		UByte = 501,
		UInt16,
		UInt24 = 509,
		UInt32 = 503,
		UInt64 = 508,
		Binary = 600,
		VarBinary,
		TinyText = 749,
		MediumText,
		LongText,
		Text,
		Guid = 800
	}
}
