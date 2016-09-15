using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace MySqlConnector.Performance.Models
{
    public class BlogPost
    {
        public static DbCommand LatestPosts(AppDb db)
        {
            var cmd = db.Connection.CreateCommand();
            cmd.CommandText = @"SELECT `Id`, `Title`, `Content` FROM `BlogPost` ORDER BY `Id` DESC LIMIT 10;";
            return cmd as MySqlCommand;
        }

        public static List<BlogPost> ReadAll(DbDataReader reader)
        {
            var posts = new List<BlogPost>();
            while (reader.Read())
            {
                var post = new BlogPost
                {
                    Id = reader.GetFieldValue<int>(0),
                    Title = reader.GetFieldValue<string>(1),
                    Content = reader.GetFieldValue<string>(2)
                };
                posts.Add(post);
            }
            reader.Dispose();
            return posts;
        }

        public static async Task<List<BlogPost>> ReadAllAsync(DbDataReader reader)
        {
            var posts = new List<BlogPost>();
            while (await reader.ReadAsync())
            {
                var post = new BlogPost
                {
                    Id = await reader.GetFieldValueAsync<int>(0),
                    Title = await reader.GetFieldValueAsync<string>(1),
                    Content = await reader.GetFieldValueAsync<string>(2)
                };
                posts.Add(post);
            }
            reader.Dispose();
            return posts;
        }

        private static MySqlCommand InsertCmd(AppDb db, BlogPost blogPost)
        {
            var cmd = db.Connection.CreateCommand() as MySqlCommand;
            cmd.CommandText = @"INSERT INTO `BlogPost` (`Title`, `Content`) VALUES (@title, @content);";
            cmd.Prepare();
            cmd.Parameters.Add("@title", DbType.String);
            cmd.Parameters.Add("@content", DbType.String);
            cmd.Parameters["@title"].Value = blogPost.Title;
            cmd.Parameters["@content"].Value = blogPost.Content;
            return cmd;
        }

        public static void Insert(AppDb db, BlogPost blogPost)
        {
            var cmd = InsertCmd(db, blogPost);
            cmd.ExecuteNonQuery();
            blogPost.Id = (int) cmd.LastInsertedId;
        }

        public static async Task InsertAsync(AppDb db, BlogPost blogPost)
        {
            var cmd = InsertCmd(db, blogPost);
            await cmd.ExecuteNonQueryAsync();
            blogPost.Id = (int) cmd.LastInsertedId;
        }

        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
    }
}
