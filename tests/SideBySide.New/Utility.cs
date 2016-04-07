using System.Data.Common;
using System.Collections.Generic;

namespace SideBySide
{
	public static class Utility
	{
		public static void Execute(this DbConnection connection, string sql, DbTransaction transaction = null)
		{
			using (var cmd = connection.CreateCommand())
			{
				cmd.CommandText = sql;
				if (transaction != null)
					cmd.Transaction = transaction;
				cmd.ExecuteNonQuery();
			}
		}

		public static IEnumerable<T> Query<T>(this DbConnection connection, string sql)
		{
			List<T> results;
			using (var cmd = connection.CreateCommand())
			{
				cmd.CommandText = sql;
				using (var reader = cmd.ExecuteReader())
				{
					do
					{
						results = new List<T>();
						while (reader.Read())
							results.Add(reader.GetFieldValue<T>(0));
					} while (reader.NextResult());
				}
			}
			return results;
		}
	}
}
