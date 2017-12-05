using System;
using System.Threading.Tasks;

namespace MySqlConnector.Utilities
{
	internal static class ValueTaskExtensions
	{
		public static async ValueTask<TResult> ContinueWith<T, TResult>(this ValueTask<T> valueTask, Func<T, ValueTask<TResult>> continuation) => await continuation(await valueTask.ConfigureAwait(false)).ConfigureAwait(false);

		public static ValueTask<T> FromException<T>(Exception exception) => new ValueTask<T>(Utility.TaskFromException<T>(exception));
	}
}
