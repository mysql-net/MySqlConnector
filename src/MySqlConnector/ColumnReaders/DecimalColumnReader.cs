using System.Buffers.Text;
using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class DecimalColumnReader : IColumnReader
{
	internal static DecimalColumnReader Instance { get; } = new DecimalColumnReader();

	public object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return Utf8Parser.TryParse(data, out decimal decimalValue, out int bytesConsumed) && bytesConsumed == data.Length ? decimalValue : throw new FormatException();
	}

	public int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		if (!Utf8Parser.TryParse(data, out decimal decimalValue, out int bytesConsumed) || bytesConsumed != data.Length)
		{
			throw new FormatException();
		}
		return (int) decimalValue;
	}
}
