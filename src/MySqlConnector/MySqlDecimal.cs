using System.Globalization;
using System.Text.RegularExpressions;
using MySqlConnector.Utilities;

namespace MySqlConnector;

public readonly struct MySqlDecimal
{
	public decimal Value => Convert.ToDecimal(m_value, CultureInfo.InvariantCulture);

	public double ToDouble() => double.Parse(m_value, CultureInfo.InvariantCulture);

	public override string ToString() => m_value;

	internal MySqlDecimal(string value)
	{
		if (s_pattern.Match(value) is { Success: true } match)
		{
			var wholeLength = match.Groups[1].Length;
			var fractionLength = match.Groups[3].Value.TrimEnd('0').Length;

			var isWithinLengthLimits = wholeLength + fractionLength <= 65 && fractionLength <= 30;
			var isNegativeZero = value[0] == '-' && match.Groups[1].Value == "0" && fractionLength == 0;
			if (isWithinLengthLimits && !isNegativeZero)
			{
				m_value = value;
				return;
			}
		}

		throw new FormatException("Could not parse the value as a MySqlDecimal: {0}".FormatInvariant(value));
	}

	private static readonly Regex s_pattern = new(@"^-?([1-9][0-9]*|0)(\.([0-9]+))?$");

	private readonly string m_value;
}
