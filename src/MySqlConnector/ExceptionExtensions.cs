using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MySqlConnector;

internal static class ExceptionExtensions
{
#if !NET8_0_OR_GREATER
	// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/ArgumentException.cs
	extension(ArgumentException)
	{
		/// <summary>Throws an exception if <paramref name="argument"/> is null or empty.</summary>
		/// <param name="argument">The string argument to validate as non-null and non-empty.</param>
		/// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
		/// <exception cref="ArgumentNullException"><paramref name="argument"/> is null.</exception>
		/// <exception cref="ArgumentException"><paramref name="argument"/> is empty.</exception>
		public static void ThrowIfNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
		{
			if (argument is null)
				throw new ArgumentNullException(paramName);
			if (argument.Length == 0)
				throw new ArgumentException("The string must not be empty.", paramName);
		}
	}
#endif

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
