// Supports using nullable attributes on older frameworks.
// Copied from https://github.com/dotnet/corefx/blob/master/src/Common/src/CoreLib/System/Diagnostics/CodeAnalysis/NullableAttributes.cs

#if NET461 || NET471 || NETSTANDARD2_0 || NETCOREAPP2_1
namespace System.Diagnostics.CodeAnalysis;

/// <summary>Specifies that null is allowed as an input even if the corresponding type disallows it.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false)]
internal sealed class AllowNullAttribute : Attribute
{
}

/// <summary>Specifies that an output will not be null even if the corresponding type allows it.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
internal sealed class NotNullAttribute : Attribute
{
}

/// <summary>Specifies that when a method returns <see cref="ReturnValue"/>, the parameter will not be null even if the corresponding type allows it.</summary>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
internal sealed class NotNullWhenAttribute : Attribute
{
	/// <summary>Initializes the attribute with the specified return value condition.</summary>
	/// <param name="returnValue">
	/// The return value condition. If the method returns this value, the associated parameter will not be null.
	/// </param>
	public NotNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;

	/// <summary>Gets the return value condition.</summary>
	public bool ReturnValue { get; }
}
#endif
