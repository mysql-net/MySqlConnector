using System;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlHelper
	{
		[Obsolete("Use MySqlConnection.ClearAllPools or MySqlConnection.ClearAllPoolsAsync")]
		public static void ClearConnectionPools() => MySqlConnection.ClearAllPools();
	}
}
