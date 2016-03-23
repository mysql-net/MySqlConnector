namespace MySql.Data.Serialization
{
	internal sealed class OkPayload
	{
		public int AffectedRowCount { get; set; }
		public int LastInsertId { get; set; }
		public ServerStatus ServerStatus { get; set; }
		public int WarningCount { get; set; }

		public static OkPayload Create(PayloadData payload)
		{
			var reader = new ByteArrayReader(payload.ArraySegment);
			reader.ReadByte(0);
			var affectedRowCount = (int) reader.ReadLengthEncodedInteger();
			var lastInsertId = (int) reader.ReadLengthEncodedInteger();
			var serverStatus = (ServerStatus) reader.ReadUInt16();
			var warningCount = (int) reader.ReadUInt16();

			return new OkPayload(affectedRowCount, lastInsertId, serverStatus, warningCount);
		}

		private OkPayload(int affectedRowCount, int lastInsertId, ServerStatus serverStatus, int warningCount)
		{
			AffectedRowCount = affectedRowCount;
			LastInsertId = lastInsertId;
			ServerStatus = serverStatus;
			WarningCount = warningCount;
		}
	}
}
