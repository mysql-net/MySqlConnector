using System.Buffers.Text;
using MySqlConnector.Protocol;
using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class BitColumnReader : IColumnReader
{
	public static BitColumnReader Instance { get; } = new();

	public object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		if ((columnDefinition.ColumnFlags & ColumnFlags.Binary) == 0)
		{
			// when the Binary flag IS NOT set, the BIT column is transmitted as MSB byte array
			ulong bitValue = 0;
			for (int i = 0; i < data.Length; i++)
				bitValue = bitValue * 256 + data[i];
			return bitValue;
		}
		else if (columnDefinition.ColumnLength <= 5 && data.Length == 1 && data[0] < (byte) (1 << (int) columnDefinition.ColumnLength))
		{
			// a server bug may return the data as binary even when we expect text: https://github.com/mysql-net/MySqlConnector/issues/713
			// in this case, the data can't possibly be an ASCII digit, so assume it's the binary serialisation of BIT(n) where n <= 5
			return (ulong) data[0];
		}
		else
		{
			// when the Binary flag IS set, the BIT column is transmitted as text
			return !Utf8Parser.TryParse(data, out ulong value, out var bytesConsumed) || bytesConsumed != data.Length ? throw new FormatException() : value;
		}
	}

	public int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		if ((columnDefinition.ColumnFlags & ColumnFlags.Binary) == 0)
		{
			// when the Binary flag IS NOT set, the BIT column is transmitted as MSB byte array
			ulong bitValue = 0;
			for (int i = 0; i < data.Length; i++)
				bitValue = bitValue * 256 + data[i];
			return checked((int) bitValue);
		}
		else if (columnDefinition.ColumnLength <= 5 && data.Length == 1 && data[0] < (byte) (1 << (int) columnDefinition.ColumnLength))
		{
			// a server bug may return the data as binary even when we expect text: https://github.com/mysql-net/MySqlConnector/issues/713
			// in this case, the data can't possibly be an ASCII digit, so assume it's the binary serialisation of BIT(n) where n <= 5
			return checked((int) data[0]);
		}
		else
		{
			// when the Binary flag IS set, the BIT column is transmitted as text
			if (!Utf8Parser.TryParse(data, out long value, out var bytesConsumed) || bytesConsumed != data.Length)
			{
				throw new FormatException();
			}
			return checked((int) value);
		}
	}
}
