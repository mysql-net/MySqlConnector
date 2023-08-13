using System.Runtime.CompilerServices;

namespace MySqlConnector.Utilities;

internal static class ValueTaskExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ValueTask FromException(Exception exception) =>
#if NET5_0_OR_GREATER
		ValueTask.FromException(exception);
#else
		new(Task.FromException(exception));
#endif

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ValueTask<T> FromException<T>(Exception exception) =>
#if NET5_0_OR_GREATER
		ValueTask.FromException<T>(exception);
#else
		new(Task.FromException<T>(exception));
#endif
}
