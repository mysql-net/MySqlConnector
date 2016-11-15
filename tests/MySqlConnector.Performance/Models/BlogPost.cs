using System.Data;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace MySqlConnector.Performance.Models
{
	public class BlogPost
	{
		public int Id { get; set; }
		public string Title { get; set; }
		public string Content { get; set; }

		[JsonIgnore]
		public AppDb Db { get; set; }

		public BlogPost(AppDb db=null)
		{
			Db = db;
		}

		public void Insert()
		{
			var cmd = InsertCmd();
			cmd.ExecuteNonQuery();
			Id = (int) cmd.LastInsertedId;
		}

		public async Task InsertAsync()
		{
			var cmd = InsertCmd();
			await cmd.ExecuteNonQueryAsync();
			Id = (int) cmd.LastInsertedId;
		}

		public void Update()
		{
			var cmd = UpdateCmd();
			cmd.ExecuteNonQuery();
		}

		public async Task UpdateAsync()
		{
			var cmd = UpdateCmd();
			await cmd.ExecuteNonQueryAsync();
		}

		public void Delete()
		{
			var cmd = DeleteCmd();
			cmd.ExecuteNonQuery();
		}

		public async Task DeleteAsync()
		{
			var cmd = DeleteCmd();
			await cmd.ExecuteNonQueryAsync();
		}

		private void BindId(MySqlCommand cmd)
		{
			cmd.Parameters.Add(new MySqlParameter
			{
				ParameterName = "@id",
				DbType = DbType.Int32,
				Value = Id,
			});
		}

		private void BindParams(MySqlCommand cmd)
		{
			cmd.Parameters.Add(new MySqlParameter
			{
				ParameterName = "@title",
				DbType = DbType.String,
				Value = Title,
			});
			cmd.Parameters.Add(new MySqlParameter
			{
				ParameterName = "@content",
				DbType = DbType.String,
				Value = Content,
			});
		}

		private MySqlCommand InsertCmd()
		{
			var cmd = Db.Connection.CreateCommand() as MySqlCommand;
			// ReSharper disable once PossibleNullReferenceException
			cmd.CommandText = @"INSERT INTO `BlogPost` (`Title`, `Content`) VALUES (@title, @content);";
			BindParams(cmd);
			return cmd;
		}

		private MySqlCommand UpdateCmd()
		{
			var cmd = Db.Connection.CreateCommand() as MySqlCommand;
			// ReSharper disable once PossibleNullReferenceException
			cmd.CommandText = @"UPDATE `BlogPost` SET `Title` = @title, `Content` = @content WHERE `Id` = @id;";
			BindParams(cmd);
			BindId(cmd);
			return cmd;
		}

		private MySqlCommand DeleteCmd()
		{
			var cmd = Db.Connection.CreateCommand() as MySqlCommand;
			// ReSharper disable once PossibleNullReferenceException
			cmd.CommandText = @"DELETE FROM `BlogPost` WHERE `Id` = @id;";
			BindId(cmd);
			return cmd;
		}

	}
}
