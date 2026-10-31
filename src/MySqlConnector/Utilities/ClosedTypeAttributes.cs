// Supports declaring closed classes on older frameworks.
namespace System.Runtime.CompilerServices;

#if !NET7_0_OR_GREATER
// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/CompilerFeatureRequiredAttribute.cs
[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
internal sealed class CompilerFeatureRequiredAttribute(string featureName) : Attribute
{
	public string FeatureName { get; } = featureName;
}
#endif

#if !NET11_0_OR_GREATER
// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/IsClosedTypeAttribute.cs
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
internal sealed class IsClosedTypeAttribute : Attribute
{
	public Type[] DerivedTypes { get; set; } = [];
}
#endif
