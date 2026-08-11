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

#if !NET8_0_OR_GREATER
	extension (ArgumentOutOfRangeException)
	{
		/// <summary>Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is negative.</summary>
		/// <param name="value">The argument to validate as non-negative.</param>
		/// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
		public static void ThrowIfNegative(int value, [CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException(paramName, "The value must not be negative.");
		}

		/// <summary>Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is negative.</summary>
		/// <param name="value">The argument to validate as non-negative.</param>
		/// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
		public static void ThrowIfNegative(long value, [CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value < 0L)
				throw new ArgumentOutOfRangeException(paramName, "The value must not be negative.");
		}

		/// <summary>Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is greater than <paramref name="other"/>.</summary>
		/// <param name="value">The argument to validate as less or equal than <paramref name="other"/>.</param>
		/// <param name="other">The value to compare with <paramref name="value"/>.</param>
		/// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
		public static void ThrowIfGreaterThan<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
			where T : IComparable<T>
		{
			if (value.CompareTo(other) > 0)
				throw new ArgumentOutOfRangeException(paramName, $"The value must be less than or equal to {other}.");
		}

		/// <summary>Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is greater than or equal <paramref name="other"/>.</summary>
		/// <param name="value">The argument to validate as less than <paramref name="other"/>.</param>
		/// <param name="other">The value to compare with <paramref name="value"/>.</param>
		/// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
		public static void ThrowIfGreaterThanOrEqual<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
			where T : IComparable<T>
		{
			if (value.CompareTo(other) >= 0)
				throw new ArgumentOutOfRangeException(paramName, $"The value must be less than {other}.");
		}

		/// <summary>Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is less than or equal <paramref name="other"/>.</summary>
		/// <param name="value">The argument to validate as greater than than <paramref name="other"/>.</param>
		/// <param name="other">The value to compare with <paramref name="value"/>.</param>
		/// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
		public static void ThrowIfLessThanOrEqual<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
			where T : IComparable<T>
		{
			if (value.CompareTo(other) <= 0)
				throw new ArgumentOutOfRangeException(paramName, $"The value must be greater than {other}.");
		}
	}
#endif
}
