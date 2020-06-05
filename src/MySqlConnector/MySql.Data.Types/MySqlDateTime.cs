using System;

namespace MySqlConnector
{
	public struct MySqlDateTime : IComparable, IConvertible
	{
		public MySqlDateTime(int year, int month, int day, int hour, int minute, int second, int microsecond)
		{
			Year = year;
			Month = month;
			Day = day;
			Hour = hour;
			Minute = minute;
			Second = second;
			Microsecond = microsecond;
		}

		public MySqlDateTime(DateTime dt)
			: this(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, (int) (dt.Ticks % 10_000_000) / 10)
		{
		}

		public MySqlDateTime(MySqlDateTime other)
		{
			Year = other.Year;
			Month = other.Month;
			Day = other.Day;
			Hour = other.Hour;
			Minute = other.Minute;
			Second = other.Second;
			Microsecond = other.Microsecond;
		}

		public readonly bool IsValidDateTime => Year != 0 && Month != 0 && Day != 0;

		public int Year { get; set; }
		public int Month { get; set; }
		public int Day { get; set; }
		public int Hour { get; set; }
		public int Minute { get; set; }
		public int Second { get; set; }
		public int Microsecond { get; set; }

		public int Millisecond
		{
			readonly get => Microsecond / 1000;
			set => Microsecond = value * 1000;
		}

		public readonly DateTime GetDateTime() =>
			!IsValidDateTime ? throw new MySqlConversionException("Cannot convert MySqlDateTime to DateTime when IsValidDateTime is false.") :
				new DateTime(Year, Month, Day, Hour, Minute, Second, DateTimeKind.Unspecified).AddTicks(Microsecond * 10);

		public readonly override string ToString() => IsValidDateTime ? GetDateTime().ToString() : "0000-00-00";

		public static explicit operator DateTime(MySqlDateTime val) => !val.IsValidDateTime ? DateTime.MinValue : val.GetDateTime();

		readonly int IComparable.CompareTo(object? obj)
		{
			if (!(obj is MySqlDateTime other))
				throw new ArgumentException("CompareTo can only be called with another MySqlDateTime", nameof(obj));

			if (Year < other.Year)
				return -1;
			if (Year > other.Year)
				return 1;
			if (Month < other.Month)
				return -1;
			if (Month > other.Month)
				return 1;
			if (Day < other.Day)
				return -1;
			if (Day > other.Day)
				return 1;
			if (Hour < other.Hour)
				return -1;
			if (Hour > other.Hour)
				return 1;
			if (Minute < other.Minute)
				return -1;
			if (Minute > other.Minute)
				return 1;
			if (Second < other.Second)
				return -1;
			if (Second > other.Second)
				return 1;
			return Microsecond.CompareTo(other.Microsecond);
		}

		DateTime IConvertible.ToDateTime(IFormatProvider? provider) => IsValidDateTime ? GetDateTime() : throw new InvalidCastException();
		string IConvertible.ToString(IFormatProvider? provider) => IsValidDateTime ? GetDateTime().ToString(provider) : "0000-00-00";

		object IConvertible.ToType(Type conversionType, IFormatProvider? provider) =>
			conversionType == typeof(DateTime) ? (object) GetDateTime() :
			conversionType == typeof(string) ? ((IConvertible) this).ToString(provider) :
			throw new InvalidCastException();

		TypeCode IConvertible.GetTypeCode() => TypeCode.Object;
		bool IConvertible.ToBoolean(IFormatProvider? provider) => throw new InvalidCastException();
		char IConvertible.ToChar(IFormatProvider? provider) => throw new InvalidCastException();
		sbyte IConvertible.ToSByte(IFormatProvider? provider) => throw new InvalidCastException();
		byte IConvertible.ToByte(IFormatProvider? provider) => throw new InvalidCastException();
		short IConvertible.ToInt16(IFormatProvider? provider) => throw new InvalidCastException();
		ushort IConvertible.ToUInt16(IFormatProvider? provider) => throw new InvalidCastException();
		int IConvertible.ToInt32(IFormatProvider? provider) => throw new InvalidCastException();
		uint IConvertible.ToUInt32(IFormatProvider? provider) => throw new InvalidCastException();
		long IConvertible.ToInt64(IFormatProvider? provider) => throw new InvalidCastException();
		ulong IConvertible.ToUInt64(IFormatProvider? provider) => throw new InvalidCastException();
		float IConvertible.ToSingle(IFormatProvider? provider) => throw new InvalidCastException();
		double IConvertible.ToDouble(IFormatProvider? provider) => throw new InvalidCastException();
		decimal IConvertible.ToDecimal(IFormatProvider? provider) => throw new InvalidCastException();
	}
}
