namespace MySqlConnector.Utilities;

internal static class ValueTaskExtensions
{
	public static ValueTask<T> FromException<T>(Exception exception) => new ValueTask<T>(Task.FromException<T>(exception));
}
