using System;
using System.Threading.Tasks;

namespace MySql.Data
{
	internal static class ValueTaskExtensions
	{
		public static async ValueTask<TResult> ContinueWith<T, TResult>(this ValueTask<T> valueTask, Func<T, ValueTask<TResult>> continuation) => await continuation(await valueTask);

		public static ValueTask<T> FromException<T>(Exception exception) => new ValueTask<T>(Utility.TaskFromException<T>(exception));
	}
}
