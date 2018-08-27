using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Protocol.Payloads
{
	internal readonly struct StatementPrepareResponsePayload
	{
		public int StatementId { get; }
		public int ColumnCount { get; }
		public int ParameterCount { get; }

		public static StatementPrepareResponsePayload Create(in PayloadData payload)
		{
			var reader = new ByteArrayReader(payload.ArraySegment);
			reader.ReadByte(0);
			var statementId = reader.ReadInt32();
			var columnCount = (int) reader.ReadInt16();
			var parameterCount = (int) reader.ReadInt16();
			reader.ReadByte(0);
			var warningCount = (int) reader.ReadInt16();

			return new StatementPrepareResponsePayload(statementId, columnCount, parameterCount);
		}

		private StatementPrepareResponsePayload(int statementId, int columnCount, int parameterCount)
		{
			StatementId = statementId;
			ColumnCount = columnCount;
			ParameterCount = parameterCount;
		}
	}
}
