using System.Text;

namespace MySqlConnector;

public sealed class MySqlHelper
{
	[Obsolete("Use MySqlConnection.ClearAllPools or MySqlConnection.ClearAllPoolsAsync")]
	public static void ClearConnectionPools() => MySqlConnection.ClearAllPools();

	/// <summary>
	/// Escapes single and double quotes, and backslashes in <paramref name="value"/>.
	/// </summary>
	public static string EscapeString(string value)
	{
		ArgumentNullException.ThrowIfNull(value);

		StringBuilder? sb = null;
		int last = -1;
		for (int i = 0; i < value.Length; i++)
		{
			if (value[i] is '\'' or '\"' or '\\')
			{
				sb ??= new();
				sb.Append(value, last + 1, i - (last + 1));
				sb.Append('\\');
				sb.Append(value[i]);
				last = i;
			}
		}
		sb?.Append(value, last + 1, value.Length - (last + 1));

		return sb?.ToString() ?? value;
	}
}
