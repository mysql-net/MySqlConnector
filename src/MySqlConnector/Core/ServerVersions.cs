using System;

namespace MySqlConnector.Core
{
	internal static class ServerVersions
	{
		// https://dev.mysql.com/doc/relnotes/mysql/5.5/en/news-5-5-3.html
        public static readonly Version SupportsUtf8Mb4 = new(5, 5, 3);

		// https://dev.mysql.com/doc/refman/5.7/en/mysql-reset-connection.html
		public static readonly Version SupportsResetConnection = new(5, 7, 3);

		// https://mariadb.com/kb/en/library/com_reset_connection/
		public static readonly Version MariaDbSupportsResetConnection = new(10, 2, 4);

		// http://dev.mysql.com/doc/refman/5.5/en/parameters-table.html
		public static readonly Version SupportsProcedureCache = new(5, 5, 3);

		// https://ocelot.ca/blog/blog/2017/08/22/no-more-mysql-proc-in-mysql-8-0/
		public static readonly Version RemovesMySqlProcTable = new(8, 0, 0);
	}
}
