using System;
using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Protocol.Payloads
{
	// See https://dev.mysql.com/doc/internals/en/packet-EOF_Packet.html
	internal sealed class EofPayload
	{
		public int WarningCount { get; }
		public ServerStatus ServerStatus { get; }

		public static EofPayload Create(ReadOnlySpan<byte> span)
		{
			var reader = new ByteArrayReader(span);
			reader.ReadByte(Signature);
			if (span.Length > 5)
				throw new FormatException("Not an EOF packet");
			int warningCount = reader.ReadUInt16();
			var serverStatus = (ServerStatus) reader.ReadUInt16();

			if (reader.BytesRemaining != 0)
				throw new FormatException("Extra bytes at end of payload.");
			return new EofPayload(warningCount, serverStatus);
		}

		/// <summary>
		/// Returns <c>true</c> if <paramref name="payload"/> contains an <a href="https://dev.mysql.com/doc/internals/en/packet-EOF_Packet.html">EOF packet</a>.
		/// Note that EOF packets can appear in places where a length-encoded integer (which starts with the same signature byte) may appear, so the length
		/// has to be checked to verify that it is an EOF packet.
		/// </summary>
		/// <param name="payload">The payload to examine.</param>
		/// <returns><c>true</c> if this is an EOF packet; otherwise, <c>false</c>.</returns>
		public static bool IsEof(PayloadData payload) =>
			payload.ArraySegment.Count > 0 && payload.ArraySegment.Count < 9 && payload.ArraySegment.Array[payload.ArraySegment.Offset] == Signature;

		public const byte Signature = 0xFE;

		private EofPayload(int warningCount, ServerStatus status)
		{
			WarningCount = warningCount;
			ServerStatus = status;
		}
	}
}
