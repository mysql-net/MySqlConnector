using System;

namespace MySql.Data.Types
{
	public struct MySqlDateTime : IComparable
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

		public bool IsValidDateTime => Year != 0 && Month != 0 && Day != 0;

		public int Year { get; set; }
		public int Month { get; set; }
		public int Day { get; set; }
		public int Hour { get; set; }
		public int Minute { get; set; }
		public int Second { get; set; }
		public int Microsecond { get; set; }

		public int Millisecond
		{
			get => Microsecond / 1000;
			set => Microsecond = value * 1000;
		}

		public DateTime GetDateTime() =>
			!IsValidDateTime ? throw new MySqlConversionException("Cannot convert MySqlDateTime to DateTime when IsValidDateTime is false.") : 
				new DateTime(Year, Month, Day, Hour, Minute, Second, DateTimeKind.Unspecified).AddTicks(Microsecond * 10);

		public override string ToString() => IsValidDateTime ? GetDateTime().ToString() : "0000-00-00";

		public static explicit operator DateTime(MySqlDateTime val) => !val.IsValidDateTime ? DateTime.MinValue : val.GetDateTime();

		int IComparable.CompareTo(object obj)
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
	}
}
