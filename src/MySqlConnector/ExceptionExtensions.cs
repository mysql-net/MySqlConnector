using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MySqlConnector;

internal static class ExceptionExtensions
{
#if !NET6_0_OR_GREATER
	// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/ArgumentNullException.cs
	extension(ArgumentNullException)
	{
		/// <summary>Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null.</summary>
		/// <param name="argument">The reference type argument to validate as non-null.</param>
		/// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
		/// <remarks>From https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/ArgumentNullException.cs.</remarks>
		public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression("argument")] string? paramName = null)
		{
			if (argument is null)
				throw new ArgumentNullException(paramName);
		}
	}
#endif
}
