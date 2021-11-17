using System.Globalization;
using System.Text.RegularExpressions;

namespace MySqlConnector;

public readonly struct MySqlDecimal
{
	private readonly string value;
	static readonly string regexWithDecimal = @"^-?([1-9][0-9]*|0)(\.[0-9]+)$";
	static readonly string regexWithOutDecimal = @"^-?([1-9][0-9]*)$";

	internal MySqlDecimal(string val)
	{

		var matchWithDecimal = Regex.Match(val, regexWithDecimal, RegexOptions.IgnoreCase);
		var matchWithOutDecimal = Regex.Match(val, regexWithOutDecimal, RegexOptions.IgnoreCase);

		if(!(matchWithDecimal.Success || matchWithOutDecimal.Success))
		{
			throw new FormatException("Format is wrong.");
		}

		bool negative = val[0] == '-';
		// If its valid negative value with . then length should be less than 68
		bool withDecimalNegative = (matchWithDecimal.Success && (val.Length >= 68 && negative));
		// If its valid positive value with . then length should be less than 67
		bool withDecimalPositive = (matchWithDecimal.Success && (val.Length >= 67 && !negative));
		// If its valid negative value without . then length should be less than 67
		bool withOutDecimalNegative = (matchWithOutDecimal.Success && (val.Length >= 67 && negative));
		// If its valid positive value without . then length should be less than 66
		bool withOutDecimalPositive = (matchWithOutDecimal.Success && (val.Length >= 66 && !negative));

		if (withDecimalNegative || withDecimalPositive || withOutDecimalNegative || withOutDecimalPositive)
		{
			throw new FormatException("Value is too large for Conversion.");
		}
		value = val;
	}

	public decimal Value => Convert.ToDecimal(value, CultureInfo.InvariantCulture);

	public double ToDouble()
	{
		return Double.Parse(value, CultureInfo.InvariantCulture);
	}

	public override string ToString() => value;
}
