using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Core;

internal sealed class TextRow : Row
{
	public TextRow(ResultSet resultSet)
		: base(false, resultSet)
	{
	}

	protected override Row CloneCore() => new TextRow(ResultSet);

	protected override void GetDataOffsets(ReadOnlySpan<byte> data, int[] dataOffsets, int[] dataLengths)
	{
		var reader = new ByteArrayReader(data);
		for (var column = 0; column < dataOffsets.Length; column++)
		{
			var length = reader.ReadLengthEncodedIntegerOrNull();
			dataLengths[column] = length == -1 ? 0 : length;
			dataOffsets[column] = length == -1 ? -1 : reader.Offset;
			reader.Offset += dataLengths[column];
		}
	}
}
