using System;

namespace MySql.Data.Serialization
{
	// See https://dev.mysql.com/doc/internals/en/com-stmt-prepare-response.html#packet-COM_STMT_PREPARE_OK
	internal class PrepareStatementOkPayload
    {
		public int ColumnCount { get; }
		public int ParameterCount { get; }
		public uint StatementId { get; }
		public int WarningCount { get; }

		public static PrepareStatementOkPayload Create(PayloadData payload)
		{
			var reader = new ByteArrayReader(payload.ArraySegment);
			reader.ReadByte(0);
			var statementId = reader.ReadUInt32();
			int columnCount = reader.ReadUInt16();
			int parameterCount = reader.ReadUInt16();
			reader.ReadByte(0);
			int warningCount = reader.ReadUInt16();

			if (reader.BytesRemaining != 0)
				throw new FormatException("Extra bytes at end of payload.");
			return new PrepareStatementOkPayload(statementId, columnCount, parameterCount, warningCount);
		}

		private PrepareStatementOkPayload(uint statementId, int columnCount, int parameterCount, int warningCount)
		{
			StatementId = statementId;
			ColumnCount = columnCount;
			ParameterCount = parameterCount;
			WarningCount = warningCount;
		}
	}
}
