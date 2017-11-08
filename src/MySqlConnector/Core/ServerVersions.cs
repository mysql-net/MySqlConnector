using System;

namespace MySqlConnector.Core
{
	internal static class ServerVersions
	{
		// https://dev.mysql.com/doc/refman/5.7/en/mysql-reset-connection.html
		public static readonly Version SupportsResetConnection = new Version(5, 7, 3);

		// http://dev.mysql.com/doc/refman/5.5/en/parameters-table.html
		public static readonly Version SupportsProcedureCache = new Version(5, 5, 3);
	}
}
