using System;

namespace MySql.Data.Serialization
{
	// See https://dev.mysql.com/doc/internals/en/packet-EOF_Packet.html
	internal class EofPayload
    {
		public int WarningCount { get; }
		public ServerStatus ServerStatus { get; }

		public static EofPayload Create(PayloadData payload)
		{
			var reader = new ByteArrayReader(payload.ArraySegment);
			reader.ReadByte(HeaderByte);
			if (payload.ArraySegment.Count > 5)
				throw new FormatException("Not an EOF packet");
			int warningCount = reader.ReadUInt16();
		    ServerStatus serverStatus = (ServerStatus) reader.ReadUInt16();

			if (reader.BytesRemaining != 0)
				throw new FormatException("Extra bytes at end of payload.");
			return new EofPayload(warningCount, serverStatus);
	    }

	    public const byte HeaderByte = 0xFE;

	    private EofPayload(int warningCount, ServerStatus status)
		{
			WarningCount = warningCount;
			ServerStatus = status;
		}
	}
}
