using System.Buffers.Text;
using System.Runtime.InteropServices;
using System.Text;
using MySqlConnector.Protocol;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core;

internal sealed class BinaryRow : Row
{
	public BinaryRow(ResultSet resultSet)
		: base(true, resultSet)
	{
	}

	protected override Row CloneCore() => new BinaryRow(ResultSet);

	protected override void GetDataOffsets(ReadOnlySpan<byte> data, int[] dataOffsets, int[] dataLengths)
	{
		Array.Clear(dataOffsets, 0, dataOffsets.Length);
		for (var column = 0; column < dataOffsets.Length; column++)
		{
			if ((data[(column + 2) / 8 + 1] & (1 << ((column + 2) % 8))) != 0)
			{
				// column is NULL
				dataOffsets[column] = -1;
			}
		}

		var reader = new ByteArrayReader(data);

		// skip packet header (1 byte) and NULL bitmap (formula for length at https://dev.mysql.com/doc/internals/en/null-bitmap.html)
		reader.Offset += 1 + (dataOffsets.Length + 7 + 2) / 8;
		for (var column = 0; column < dataOffsets.Length; column++)
		{
			if (dataOffsets[column] == -1)
			{
				dataLengths[column] = 0;
			}
			else
			{
				var columnDefinition = ResultSet.ColumnDefinitions![column];
				var length = columnDefinition.ColumnType switch
				{
					ColumnType.Longlong or ColumnType.Double => 8,
					ColumnType.Long or ColumnType.Int24 or ColumnType.Float => 4,
					ColumnType.Short or ColumnType.Year => 2,
					ColumnType.Tiny => 1,
					ColumnType.Date or ColumnType.DateTime or ColumnType.NewDate or ColumnType.Timestamp or ColumnType.Time => reader.ReadByte(),
					ColumnType.DateTime2 or ColumnType.Timestamp2 => throw new NotSupportedException($"ColumnType {columnDefinition.ColumnType} is not supported"),
					_ => checked((int) reader.ReadLengthEncodedInteger()),
				};

				dataLengths[column] = length;
				dataOffsets[column] = reader.Offset;
			}

			reader.Offset += dataLengths[column];
		}
	}
}
