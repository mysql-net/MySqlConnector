namespace MySqlConnector.Utilities;

internal static class ValueTaskExtensions
{
	public static ValueTask FromException(Exception exception) => new(Task.FromException(exception));

	public static ValueTask<T> FromException<T>(Exception exception) => new(Task.FromException<T>(exception));
}
