namespace MySqlConnector.Core;

internal sealed class CachedParameter
{
	public CachedParameter(int ordinalPosition, string? mode, string name, string dataType, bool unsigned, int length, MySqlGuidFormat guidFormat)
	{
		Position = ordinalPosition;
		if (Position == 0)
			Direction = ParameterDirection.ReturnValue;
		else if (string.Equals(mode, "in", StringComparison.OrdinalIgnoreCase))
			Direction = ParameterDirection.Input;
		else if (string.Equals(mode, "inout", StringComparison.OrdinalIgnoreCase))
			Direction = ParameterDirection.InputOutput;
		else if (string.Equals(mode, "out", StringComparison.OrdinalIgnoreCase))
			Direction = ParameterDirection.Output;
		Name = name;
		MySqlDbType = TypeMapper.Instance.GetMySqlDbType(dataType, unsigned, length, guidFormat);
		Length = length;
	}

	public int Position { get; }
	public ParameterDirection Direction { get; }
	public string Name { get; }
	public MySqlDbType MySqlDbType { get; }
	public int Length { get; }
}
