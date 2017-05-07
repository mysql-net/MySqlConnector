using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace MySqlConnector.Performance.Models
{
	public class BlogPostQuery
	{

		public readonly AppDb Db;
		public BlogPostQuery(AppDb db)
		{
			Db = db;
		}

		public BlogPost FindOne(int id)
		{
			var result = ReadAll(FindOneCmd(id).ExecuteReader());
			return result.Count > 0 ? result[0] : null;
		}

		public async Task<BlogPost> FindOneAsync(int id)
		{
			var result = await ReadAllAsync(await FindOneCmd(id).ExecuteReaderAsync());
			return result.Count > 0 ? result[0] : null;
		}

		public List<BlogPost> LatestPosts()
		{
			return ReadAll(LatestPostsCmd(10).ExecuteReader());
		}

		public async Task<List<BlogPost>> LatestPostsAsync()
		{
			return await ReadAllAsync(await LatestPostsCmd(10).ExecuteReaderAsync());
		}

		public void DeleteAll()
		{
			var txn = Db.Connection.BeginTransaction();
			try
			{
				DeleteAllCmd().ExecuteNonQuery();
				txn.Commit();
			}
			catch
			{
				txn.Rollback();
				throw;
			}
		}

		public async Task DeleteAllAsync()
		{
			var txn = await Db.Connection.BeginTransactionAsync();
			try
			{
				await DeleteAllCmd().ExecuteNonQueryAsync();
#if BASELINE
				txn.Commit();
#else
				await txn.CommitAsync();
#endif
			}
			catch
			{
#if BASELINE
				txn.Rollback();
#else
				await txn.RollbackAsync();
#endif
				throw;
			}
		}

		private DbCommand FindOneCmd(int id)
		{
			var cmd = Db.Connection.CreateCommand();
			cmd.CommandText = @"SELECT `Id`, `Title`, `Content` FROM `BlogPost` WHERE `Id` = @id";
			cmd.Parameters.Add(new MySqlParameter
			{
				ParameterName = "@id",
				DbType = DbType.Int32,
				Value = id,
			});
			return cmd as MySqlCommand;
		}

		public DbCommand LatestPostsCmd(int limit)
		{
			var cmd = Db.Connection.CreateCommand();
			cmd.CommandText = @"SELECT `Id`, `Title`, `Content` FROM `BlogPost` ORDER BY `Id` DESC LIMIT @limit;";
			cmd.Parameters.Add(new MySqlParameter
			{
				ParameterName = "@limit",
				DbType = DbType.Int32,
				Value = limit,
			});
			return cmd as MySqlCommand;
		}

		private DbCommand DeleteAllCmd()
		{
			var cmd = Db.Connection.CreateCommand();
			cmd.CommandText = @"DELETE FROM `BlogPost`";
			return cmd as MySqlCommand;
		}

		private List<BlogPost> ReadAll(DbDataReader reader)
		{
			var posts = new List<BlogPost>();
			using (reader)
			{
				while (reader.Read())
				{
					var post = new BlogPost(Db)
					{
						Id = reader.GetFieldValue<int>(0),
						Title = reader.GetFieldValue<string>(1),
						Content = reader.GetFieldValue<string>(2)
					};
					posts.Add(post);
				}
			}
			return posts;
		}

		private async Task<List<BlogPost>> ReadAllAsync(DbDataReader reader)
		{
			var posts = new List<BlogPost>();
			using (reader)
			{
				while (await reader.ReadAsync())
				{
					var post = new BlogPost(Db)
					{
						Id = await reader.GetFieldValueAsync<int>(0),
						Title = await reader.GetFieldValueAsync<string>(1),
						Content = await reader.GetFieldValueAsync<string>(2)
					};
					posts.Add(post);
				}
			}
			return posts;
		}
	}
}
