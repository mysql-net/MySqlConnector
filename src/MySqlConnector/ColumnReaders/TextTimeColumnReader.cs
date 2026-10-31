using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Utilities;

namespace MySqlConnector.ColumnReaders;

internal sealed class TextTimeColumnReader : TimeColumnReader
{
	public static TextTimeColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		ReadTimeSpan(data, columnDefinition);

	public override TimeSpan ReadTimeSpan(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		Utility.ParseTimeSpan(data);
}
