using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MySqlConnector;

public readonly struct MySqlDecimal
{
	private readonly string value;

	internal MySqlDecimal(string val)
	{
		string regexWithDecimal = @"^-?([1-9][0-9]*|0)(\.[0-9]+)?$";
		string regexWithOutDecimal = @"^-?([1-9][0-9]*|0)$";
		var matchWithDecimal = Regex.Match(val, regexWithDecimal, RegexOptions.IgnoreCase);
		var matchWithOutDecimal = Regex.Match(val, regexWithOutDecimal, RegexOptions.IgnoreCase);

		if (!(matchWithDecimal.Success && val.Length <= 66) && !(matchWithOutDecimal.Success && val.Length <= 67))
		{
			throw new MySqlConversionException("Value is too large for Conversion or Format is wrong.");
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
