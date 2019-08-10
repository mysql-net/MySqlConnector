using System;
using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Protocol.Payloads
{
	internal sealed class StatementPrepareResponsePayload
	{
		public int StatementId { get; }
		public int ColumnCount { get; }
		public int ParameterCount { get; }

		public static StatementPrepareResponsePayload Create(ReadOnlySpan<byte> span)
		{
			var reader = new ByteArrayReader(span);
			reader.ReadByte(0);
			var statementId = reader.ReadInt32();
			var columnCount = (int) reader.ReadUInt16();
			var parameterCount = (int) reader.ReadUInt16();
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
