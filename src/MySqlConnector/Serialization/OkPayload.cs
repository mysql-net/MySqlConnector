namespace MySql.Data.Serialization
{
	internal sealed class OkPayload
	{
		public int AffectedRowCount { get; set; }
		public long LastInsertId { get; set; }
		public ServerStatus ServerStatus { get; set; }
		public int WarningCount { get; set; }

		public const byte Signature = 0x00;

		public static OkPayload Create(PayloadData payload)
		{
			var reader = new ByteArrayReader(payload.ArraySegment);
			reader.ReadByte(Signature);
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
