using System.Text.RegularExpressions;

namespace MySqlConnector.Core;

internal sealed partial class NormalizedSchema
{
	private const string ReQuoted = @"`((?:[^`]|``)+)`";
	private const string ReUnQuoted = @"([^\.`]+)";
	private const string ReEither = $@"(?:{ReQuoted}|{ReUnQuoted})";
	private const string ReName = $@"^\s*{ReEither}\s*(?:\.\s*{ReEither}\s*)?$";

#if NET7_0_OR_GREATER
	[GeneratedRegex(ReName)]
	private static partial Regex NameRegex();
#else
	private static Regex NameRegex() => s_nameRegex;
	private static readonly Regex s_nameRegex = new(ReName, RegexOptions.Compiled);
#endif

	public static NormalizedSchema MustNormalize(string name, string? defaultSchema = null)
	{
		var normalized = new NormalizedSchema(name, defaultSchema);
		if (normalized.Component is null)
			throw new ArgumentException("Could not determine function/procedure name", nameof(name));
		if (normalized.Schema is null)
			throw new ArgumentException("Could not determine schema", nameof(defaultSchema));
		return normalized;
	}

	public NormalizedSchema(string name, string? defaultSchema = null)
	{
		var match = NameRegex().Match(name);
		if (match.Success)
		{
			if (match.Groups[3].Success)
				Component = match.Groups[3].Value.Replace("``", "`").Trim();
			else if (match.Groups[4].Success)
				Component = match.Groups[4].Value.Trim();

			string firstGroup = "";
			if (match.Groups[1].Success)
				firstGroup = match.Groups[1].Value.Replace("``", "`").Trim();
			else if (match.Groups[2].Success)
				firstGroup = match.Groups[2].Value.Trim();
			if (Component is null)
				Component = firstGroup.Trim();
			else
				Schema = firstGroup.Trim();

			Schema ??= defaultSchema;
		}
	}

	public string? Schema { get; }
	public string? Component { get; }

	public string FullyQualified => $"`{Schema}`.`{Component}`";
}
