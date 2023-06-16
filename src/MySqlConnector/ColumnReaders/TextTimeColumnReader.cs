using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Utilities;

namespace MySqlConnector.ColumnReaders;

internal sealed class TextTimeColumnReader : ColumnReader
{
	public static TextTimeColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return Utility.ParseTimeSpan(data);
	}
}
