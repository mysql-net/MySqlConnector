using System;

namespace MySqlConnector.Protocol
{
	// From http://dev.mysql.com/doc/refman/5.7/en/c-api-data-structures.html.
	[Flags]
	internal enum ColumnFlags
	{
		/// <summary>
		/// Field cannot be <c>NULL</c>.
		/// </summary>
		NotNull = 1,

		/// <summary>
		/// Field is part of a primary key.
		/// </summary>
		PrimaryKey = 2,

		/// <summary>
		/// Field is part of a unique key.
		/// </summary>
		UniqueKey = 4,

		/// <summary>
		/// Field is part of a nonunique key.
		/// </summary>
		MultipleKey = 8,

		/// <summary>
		/// Field is a <c>BLOB</c> or <c>TEXT</c> (deprecated).
		/// </summary>
		Blob = 0x10,

		/// <summary>
		/// Field has the <c>UNSIGNED</c> attribute.
		/// </summary>
		Unsigned = 0x20,

		/// <summary>
		/// Field has the <c>ZEROFILL</c> attribute.
		/// </summary>
		ZeroFill = 0x40,

		/// <summary>
		/// Field has the <c>BINARY</c> attribute.
		/// </summary>
		Binary = 0x80,

		/// <summary>
		/// Field is an <c>ENUM</c>.
		/// </summary>
		Enum = 0x100,

		/// <summary>
		/// Field has the <c>AUTO_INCREMENT</c> attribute.
		/// </summary>
		AutoIncrement = 0x200,

		/// <summary>
		/// Field is a <c>TIMESTAMP</c> (deprecated).
		/// </summary>
		Timestamp = 0x400,

		/// <summary>
		/// Field is a <c>SET</c>.
		/// </summary>
		Set = 0x800,

		/// <summary>
		/// Field is numeric.
		/// </summary>
		Number = 0x8000,
	}
}
