using System.Text;

namespace MySqlConnector.Utilities;

internal static class StoredProcedureUtils
{
	public static string Format(string commandText, int parameterCount)
	{
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
			return string.Create(commandText.Length + 7 + parameterCount * 2 + (parameterCount == 0 ? 1 : 0), (commandText, parameterCount), static (buffer, state) =>
			{
				buffer[0] = 'C';
				buffer[1] = 'A';
				buffer[2] = 'L';
				buffer[3] = 'L';
				buffer[4] = ' ';
				buffer = buffer[5..];
				state.commandText.AsSpan().CopyTo(buffer);
				buffer = buffer[state.commandText.Length..];
				buffer[0] = '(';
				buffer = buffer[1..];
				if (state.parameterCount > 0)
				{
					buffer[0] = '?';
					buffer = buffer[1..];
					for (var i = 1; i < state.parameterCount; i++)
					{
						buffer[0] = ',';
						buffer[1] = '?';
						buffer = buffer[2..];
					}
				}
				buffer[0] = ')';
				buffer[1] = ';';
			});
#else
		var callStatement = new StringBuilder("CALL ", commandText.Length + 8 + parameterCount * 2);
		callStatement.Append(commandText);
		callStatement.Append('(');
		for (int i = 0; i < parameterCount; i++)
			callStatement.Append("?,");
		if (parameterCount == 0)
			callStatement.Append(')');
		else
			callStatement[callStatement.Length - 1] = ')';
		callStatement.Append(';');
		return callStatement.ToString();
#endif
	}
}
