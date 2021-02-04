using System;

namespace MySqlConnector
{
	/// <summary>
	/// Represents a MySQL date/time value. This type can be used to store <c>DATETIME</c> values such
	/// as <c>0000-00-00</c> that can be stored in MySQL (when <see cref="MySqlConnectionStringBuilder.AllowZeroDateTime"/>
	/// is true) but can't be stored in a <see cref="DateTime"/> value.
	/// </summary>
	public struct MySqlDateTime : IComparable, IComparable<MySqlDateTime>, IConvertible, IEquatable<MySqlDateTime>
	{
		/// <summary>
		/// Initializes a new instance of <see cref="MySqlDateTime"/>.
		/// </summary>
		/// <param name="year">The year.</param>
		/// <param name="month">The (one-based) month.</param>
		/// <param name="day">The (one-based) day of the month.</param>
		/// <param name="hour">The hour.</param>
		/// <param name="minute">The minute.</param>
		/// <param name="second">The second.</param>
		/// <param name="microsecond">The microsecond.</param>
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

		/// <summary>
		/// Initializes a new instance of <see cref="MySqlDateTime"/> from a <see cref="DateTime"/>.
		/// </summary>
		/// <param name="dt">The <see cref="DateTime"/> whose values will be copied.</param>
		public MySqlDateTime(DateTime dt)
			: this(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, (int) (dt.Ticks % 10_000_000) / 10)
		{
		}

		/// <summary>
		/// Initializes a new instance of <see cref="MySqlDateTime"/> from another <see cref="MySqlDateTime"/>.
		/// </summary>
		/// <param name="other">The <see cref="MySqlDateTime"/> whose values will be copied.</param>
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

		/// <summary>
		/// Returns <c>true</c> if this value is a valid <see cref="DateTime"/>.
		/// </summary>
		public readonly bool IsValidDateTime => Year != 0 && Month != 0 && Day != 0;

		/// <summary>
		/// Gets or sets the year.
		/// </summary>
		public int Year { get; set; }

		/// <summary>
		/// Gets or sets the month.
		/// </summary>
		public int Month { get; set; }

		/// <summary>
		/// Gets or sets the day of the month.
		/// </summary>
		public int Day { get; set; }

		/// <summary>
		/// Gets or sets the hour.
		/// </summary>
		public int Hour { get; set; }

		/// <summary>
		/// Gets or sets the minute.
		/// </summary>
		public int Minute { get; set; }

		/// <summary>
		/// Gets or sets the second.
		/// </summary>
		public int Second { get; set; }

		/// <summary>
		/// Gets or sets the microseconds.
		/// </summary>
		public int Microsecond { get; set; }

		/// <summary>
		/// Gets or sets the milliseconds.
		/// </summary>
		public int Millisecond
		{
			readonly get => Microsecond / 1000;
			set => Microsecond = value * 1000;
		}

		/// <summary>
		/// Returns a <see cref="DateTime"/> value (if <see cref="IsValidDateTime"/> is <c>true</c>), or throws a
		/// <see cref="MySqlConversionException"/>.
		/// </summary>
		public readonly DateTime GetDateTime() =>
			!IsValidDateTime ? throw new MySqlConversionException("Cannot convert MySqlDateTime to DateTime when IsValidDateTime is false.") :
				new DateTime(Year, Month, Day, Hour, Minute, Second, DateTimeKind.Unspecified).AddTicks(Microsecond * 10);

		/// <summary>
		/// Converts this object to a <see cref="String"/>.
		/// </summary>
		public readonly override string ToString() => IsValidDateTime ? GetDateTime().ToString() : "0000-00-00";

		/// <summary>
		/// Converts this object to a <see cref="DateTime"/>.
		/// </summary>
		/// <param name="val"></param>
		public static explicit operator DateTime(MySqlDateTime val) => !val.IsValidDateTime ? DateTime.MinValue : val.GetDateTime();

		/// <summary>
		/// Returns <c>true</c> if this <see cref="MySqlDateTime"/> is equal to <paramref name="obj"/>.
		/// </summary>
		/// <param name="obj">The object to compare against for equality.</param>
		/// <returns><c>true</c> if the objects are equal, otherwise <c>false</c>.</returns>
		public override bool Equals(object? obj) =>
			obj is MySqlDateTime other && ((IEquatable<MySqlDateTime>) this).Equals(other);

		/// <summary>
		/// Returns a hash code for this instance.
		/// </summary>
		public override int GetHashCode() =>
#if NET45 || NET461 || NET471 || NETSTANDARD1_3 || NETSTANDARD2_0
			(((((Year * 33 ^ Month) * 33 ^ Day) * 33 ^ Hour) * 33 ^ Minute) * 33 ^ Second) * 33 ^ Microsecond;
#else
			HashCode.Combine(Year, Month, Day, Hour, Minute, Second, Microsecond);
#endif

		public static bool operator ==(MySqlDateTime left, MySqlDateTime right) => ((IComparable<MySqlDateTime>) left).CompareTo(right) == 0;
		public static bool operator !=(MySqlDateTime left, MySqlDateTime right) => ((IComparable<MySqlDateTime>) left).CompareTo(right) != 0;
		public static bool operator <(MySqlDateTime left, MySqlDateTime right) => ((IComparable<MySqlDateTime>) left).CompareTo(right) < 0;
		public static bool operator <=(MySqlDateTime left, MySqlDateTime right) => ((IComparable<MySqlDateTime>) left).CompareTo(right) <= 0;
		public static bool operator >(MySqlDateTime left, MySqlDateTime right) => ((IComparable<MySqlDateTime>) left).CompareTo(right) > 0;
		public static bool operator >=(MySqlDateTime left, MySqlDateTime right) => ((IComparable<MySqlDateTime>) left).CompareTo(right) >= 0;

		/// <summary>
		/// Compares this object to another <see cref="MySqlDateTime"/>.
		/// </summary>
		/// <param name="obj">The object to compare to.</param>
		/// <returns>An <see cref="Int32"/> giving the results of the comparison: a negative value if this
		/// object is less than <paramref name="obj"/>, zero if this object is equal, or a positive value if this
		/// object is greater.</returns>
		readonly int IComparable.CompareTo(object? obj) =>
			obj is MySqlDateTime other ?
				((IComparable<MySqlDateTime>) this).CompareTo(other) :
				throw new ArgumentException("CompareTo can only be called with another MySqlDateTime", nameof(obj));

		/// <summary>
		/// Compares this object to another <see cref="MySqlDateTime"/>.
		/// </summary>
		/// <param name="other">The <see cref="MySqlDateTime"/> to compare to.</param>
		/// <returns>An <see cref="Int32"/> giving the results of the comparison: a negative value if this
		/// object is less than <paramref name="other"/>, zero if this object is equal, or a positive value if this
		/// object is greater.</returns>
		readonly int IComparable<MySqlDateTime>.CompareTo(MySqlDateTime other)
		{
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

		readonly bool IEquatable<MySqlDateTime>.Equals(MySqlDateTime other) => ((IComparable<MySqlDateTime>) this).CompareTo(other) == 0;

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
