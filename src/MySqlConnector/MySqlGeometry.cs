using System;
using System.Buffers.Binary;

namespace MySqlConnector
{
	/// <summary>
	/// Represents MySQL's internal GEOMETRY format: https://dev.mysql.com/doc/refman/8.0/en/gis-data-formats.html#gis-internal-format
	/// </summary>
	public sealed class MySqlGeometry
	{
		/// <summary>
		/// Constructs a <see cref="MySqlGeometry"/> from a SRID and Well-known Binary bytes.
		/// </summary>
		/// <param name="srid">The SRID (Spatial Reference System ID).</param>
		/// <param name="wkb">The Well-known Binary serialization of the geometry.</param>
		/// <returns>A new <see cref="MySqlGeometry"/> containing the specified geometry.</returns>
		public static MySqlGeometry FromWkb(int srid, ReadOnlySpan<byte> wkb)
		{
			var bytes = new byte[wkb.Length + 4];
			BinaryPrimitives.WriteInt32LittleEndian(bytes, srid);
			wkb.CopyTo(bytes.AsSpan().Slice(4));
			return new MySqlGeometry(bytes);
		}

		/// <summary>
		/// Constructs a <see cref="MySqlGeometry"/> from MySQL's internal format.
		/// </summary>
		/// <param name="value">The raw bytes of MySQL's internal GEOMETRY format.</param>
		/// <returns>A new <see cref="MySqlGeometry"/> containing the specified geometry.</returns>
		/// <remarks>See <a href="https://dev.mysql.com/doc/refman/8.0/en/gis-data-formats.html#gis-internal-format">Internal Geometry Storage Format</a>.</remarks>
		public static MySqlGeometry FromMySql(ReadOnlySpan<byte> value) => new MySqlGeometry(value.ToArray());

		/// <summary>
		/// The Spatial Reference System ID of this geometry.
		/// </summary>
		public int SRID => BinaryPrimitives.ReadInt32LittleEndian(m_bytes);

		/// <summary>
		/// The Well-known Binary serialization of this geometry.
		/// </summary>
		public ReadOnlySpan<byte> WKB => ValueSpan.Slice(4);

		/// <summary>
		/// The internal MySQL form of this geometry.
		/// </summary>
		public byte[] Value => ValueSpan.ToArray();

		internal ReadOnlySpan<byte> ValueSpan => m_bytes;

		internal MySqlGeometry(byte[] bytes) => m_bytes = bytes;

		readonly byte[] m_bytes;
	}
}
