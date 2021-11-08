using System;
using System.Globalization;

namespace MySqlConnector;

	public struct MySqlDecimal : IComparable, IComparable<MySqlDecimal>, IConvertible, IEquatable<MySqlDecimal>
	{
		private readonly string value;

		internal MySqlDecimal(string val)
		{
			if (val.Length > 65)
			{
				throw new MySqlConversionException("Value is too large for Conversion");
			}
			value = val;
		}

		public decimal Value => Convert.ToDecimal(value, CultureInfo.InvariantCulture);

		public int CompareTo(object? obj) => obj is MySqlDecimal other ?
			((IComparable<MySqlDecimal>) this).CompareTo(other) :
			throw new ArgumentException("CompareTo can only be called with another MySqlDateTime", nameof(obj));

		public int CompareTo(MySqlDecimal other)
		{
			if(other.value == value)
			{
				return 1;
			}
			else
			{
				return -1;
			}
		}

		public bool Equals(MySqlDecimal other)
		{
			return value == other.value;
		}
		public TypeCode GetTypeCode()
		{
			return TypeCode.Decimal;
		}
		public bool ToBoolean(IFormatProvider? provider) => throw new InvalidCastException();
		public byte ToByte(IFormatProvider? provider)
		{
			return Byte.Parse(value, provider);
		}
		public char ToChar(IFormatProvider? provider) => throw new InvalidCastException();
		public DateTime ToDateTime(IFormatProvider? provider) => throw new InvalidCastException();
		public decimal ToDecimal(IFormatProvider? provider)
		{
			return Decimal.Parse(value, provider);
		}

		public double ToDouble()
		{
			return Double.Parse(value, CultureInfo.InvariantCulture);
		}

		public double ToDouble(IFormatProvider? provider)
		{
			return Double.Parse(value, provider);
		}
		public short ToInt16(IFormatProvider? provider)
		{
			return Int16.Parse(value, provider);
		}
		public int ToInt32(IFormatProvider? provider)
		{
			return Int32.Parse(value, provider);
		}
		public long ToInt64(IFormatProvider? provider)
		{
			return Int64.Parse(value, provider);
		}
		public sbyte ToSByte(IFormatProvider? provider)
		{
			return SByte.Parse(value, provider);
		}
		public float ToSingle(IFormatProvider? provider)
		{
			return Single.Parse(value, provider);
		}
		public override string ToString() => value;
		public string ToString(IFormatProvider? provider)
		{
			return value;
		}
		public object ToType(Type conversionType, IFormatProvider? provider) => throw new InvalidCastException();
		public ushort ToUInt16(IFormatProvider? provider)
		{
			return UInt16.Parse(value, provider);
		}
		public uint ToUInt32(IFormatProvider? provider)
		{
			return UInt32.Parse(value, provider);
		}
		public ulong ToUInt64(IFormatProvider? provider)
		{
			return UInt64.Parse(value, provider);
		}
	}
