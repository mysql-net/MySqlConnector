namespace MySql.Data.MySqlClient
{
	public sealed class MySqlHelper
	{
		public static void ClearConnectionPools() => ConnectionPool.ClearPools();
	}
}
