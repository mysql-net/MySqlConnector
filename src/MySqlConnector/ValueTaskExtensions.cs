using System;
using System.Threading.Tasks;

namespace MySql.Data
{
	internal static class ValueTaskExtensions
	{
		public static ValueTask<TResult> ContinueWith<T, TResult>(this ValueTask<T> valueTask, Func<T, ValueTask<TResult>> continuation)
		{
			return valueTask.IsCompleted ? continuation(valueTask.Result) :
				new ValueTask<TResult>(valueTask.AsTask().ContinueWith(task => continuation(task.Result).AsTask()).Unwrap());
		}

		public static ValueTask<T> FromException<T>(Exception exception)
		{
			var tcs = new TaskCompletionSource<T>();
			tcs.SetException(exception);
			return new ValueTask<T>(tcs.Task);
		}
	}
}
