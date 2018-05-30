using System;

namespace MySql.Data.MySqlClient
{
	/// <summary>
	/// The <see cref="DateTimeKind" /> used when reading <see cref="DateTime" /> from the database.
	/// </summary>
	public enum MySqlDateTimeKind
	{
		/// <summary>
		/// Use <see cref="DateTimeKind.Unspecified" /> when reading; allow any <see cref="DateTimeKind" /> in command parameters.
		/// </summary>
		Unspecified = DateTimeKind.Unspecified,

		/// <summary>
		/// Use <see cref="DateTimeKind.Utc" /> when reading; reject <see cref="DateTimeKind.Local" /> in command parameters.
		/// </summary>
		Utc = DateTimeKind.Utc,

		/// <summary>
		/// Use <see cref="DateTimeKind.Local" /> when reading; reject <see cref="DateTimeKind.Utc" /> in command parameters.
		/// </summary>
		Local = DateTimeKind.Local,
	}
}
