using System;

namespace MySql.Data.MySqlClient
{
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
		UInt32,
		UInt64 = 508,
		UInt24,
		Binary = 600,
		VarBinary,
		TinyText = 749,
		MediumText,
		LongText,
		Text,
		Guid = 800
	}
}
