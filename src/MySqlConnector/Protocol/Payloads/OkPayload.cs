using System;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector.Protocol.Payloads
{
	internal sealed class OkPayload
	{
		public int AffectedRowCount { get; set; }
		public long LastInsertId { get; set; }
		public ServerStatus ServerStatus { get; set; }
		public int WarningCount { get; set; }

		public const byte Signature = 0x00;

		/* See
		 * http://web.archive.org/web/20160604101747/http://dev.mysql.com/doc/internals/en/packet-OK_Packet.html
		 * https://mariadb.com/kb/en/the-mariadb-library/resultset/
		 * https://github.com/MariaDB/mariadb-connector-j/blob/5fa814ac6e1b4c9cb6d141bd221cbd5fc45c8a78/src/main/java/org/mariadb/jdbc/internal/com/read/resultset/SelectResultSet.java#L443-L444
		 */
		public static bool IsOk(PayloadData payload, bool deprecateEof) =>
			payload.ArraySegment.Array != null && payload.ArraySegment.Count > 0 &&
				((payload.ArraySegment.Count > 6 && payload.ArraySegment.Array[payload.ArraySegment.Offset] == Signature) ||
				(deprecateEof && payload.ArraySegment.Count < 0xFF_FFFF && payload.ArraySegment.Array[payload.ArraySegment.Offset] == EofPayload.Signature));

		public static OkPayload Create(PayloadData payload) => Create(payload, false);

		public static OkPayload Create(PayloadData payload, bool deprecateEof)
		{
			var reader = new ByteArrayReader(payload.ArraySegment);
			var signature = reader.ReadByte();
			if (signature != Signature && (!deprecateEof || signature != EofPayload.Signature))
				throw new FormatException("Expected to read 0x00 or 0xFE but got 0x{0:X2}".FormatInvariant(signature));
			var affectedRowCount = checked((int) reader.ReadLengthEncodedInteger());
			var lastInsertId = checked((long) reader.ReadLengthEncodedInteger());
			var serverStatus = (ServerStatus) reader.ReadUInt16();
			var warningCount = (int) reader.ReadUInt16();

			return new OkPayload(affectedRowCount, lastInsertId, serverStatus, warningCount);
		}

		private OkPayload(int affectedRowCount, long lastInsertId, ServerStatus serverStatus, int warningCount)
		{
			AffectedRowCount = affectedRowCount;
			LastInsertId = lastInsertId;
			ServerStatus = serverStatus;
			WarningCount = warningCount;
		}
	}
}
