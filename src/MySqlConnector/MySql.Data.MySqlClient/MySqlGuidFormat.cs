using System;

namespace MySql.Data.MySqlClient
{
	/// <summary>
	/// Determines which column type (if any) should be read as a <c>System.Guid</c>.
	/// </summary>
	public enum MySqlGuidFormat
	{
		/// <summary>
		/// Same as <c>Char36</c> if <c>OldGuids=False</c>; same as <c>LittleEndianBinary16</c> if <c>OldGuids=True</c>.
		/// </summary>
		Default,

		/// <summary>
		/// No column types are read/written as a <code>Guid</code>.
		/// </summary>
		None,

		/// <summary>
		/// All <c>CHAR(36)</c> columns are read/written as a <c>Guid</c> using lowercase hex with hyphens,
		/// which matches <a href="https://dev.mysql.com/doc/refman/8.0/en/miscellaneous-functions.html#function_uuid"><c>UUID()</c></a>.
		/// </summary>
		Char36,

		/// <summary>
		/// All <c>CHAR(32)</c> columns are read/written as a <c>Guid</c> using lowercase hex without hyphens.
		/// </summary>
		Char32,

		/// <summary>
		/// All <c>BINARY(16)</c> columns are read/written as a <c>Guid</c> using big-endian byte order,
		/// which matches <a href="https://dev.mysql.com/doc/refman/8.0/en/miscellaneous-functions.html#function_uuid-to-bin"><c>UUID_TO_BIN(x)</c></a>.
		/// </summary>
		Binary16,

		/// <summary>
		/// All <c>BINARY(16)</c> columns are read/written as a <c>Guid</c> using big-endian byte order with time parts swapped,
		/// which matches <a href="https://dev.mysql.com/doc/refman/8.0/en/miscellaneous-functions.html#function_uuid-to-bin"><c>UUID_TO_BIN(x,1)</c></a>.
		/// </summary>
		TimeSwapBinary16,

		/// <summary>
		/// All <c>BINARY(16)</c> columns are read/written as a <c>Guid</c> using little-endian byte order, i.e. the byte order
		/// used by <see cref="Guid.ToByteArray()"/> and <see cref="Guid(byte[])"/>.
		/// </summary>
		LittleEndianBinary16,
	}
}
