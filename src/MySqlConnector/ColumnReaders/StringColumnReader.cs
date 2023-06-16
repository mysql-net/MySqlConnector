using System.Text;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Utilities;

namespace MySqlConnector.ColumnReaders;

internal sealed class StringColumnReader : ColumnReader
{
	public static StringColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return Encoding.UTF8.GetString(data);
	}
}
