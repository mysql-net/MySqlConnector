using System;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector.Performance.Models;

namespace MySqlConnector.Performance.Controllers
{
	[Route("api/[controller]")]
	public class SyncController : Controller
	{
		// GET api/sync
		[HttpGet]
		public IActionResult GetLatest()
		{
			using (var db = new AppDb())
			{
				db.Connection.Open();
				var query = new BlogPostQuery(db);
				var result = query.LatestPosts();
				return new OkObjectResult(result);
			}
		}

		// GET api/sync/5
		[HttpGet("{id}")]
		public IActionResult GetOne(int id)
		{
			using (var db = new AppDb())
			{
				db.Connection.Open();
				var query = new BlogPostQuery(db);
				var result = query.FindOne(id);
				if (result == null)
					return new NotFoundResult();
				return new OkObjectResult(result);
			}
		}

		// POST api/sync
		[HttpPost]
		public IActionResult Post([FromBody]BlogPost body)
		{
			using (var db = new AppDb())
			{
				db.Connection.Open();
				body.Db = db;
				body.Insert();
				return new OkObjectResult(body);
			}
		}

		// PUT api/sync/5
		[HttpPut("{id}")]
		public IActionResult PutOne(int id, [FromBody]BlogPost body)
		{
			using (var db = new AppDb())
			{
				db.Connection.Open();
				var query = new BlogPostQuery(db);
				var result = query.FindOne(id);
				if (result == null)
					return new NotFoundResult();
				result.Title = body.Title;
				result.Content = body.Content;
				result.Update();
				return new OkObjectResult(result);
			}
		}

		// DELETE api/sync/5
		[HttpDelete("{id}")]
		public IActionResult DeleteOne(int id)
		{
			using (var db = new AppDb())
			{
				db.Connection.Open();
				var query = new BlogPostQuery(db);
				var result = query.FindOne(id);
				if (result == null)
					return new NotFoundResult();
				result.Delete();
				return new OkResult();
			}
		}

		// DELETE api/sync
		[HttpDelete]
		public IActionResult DeleteAll()
		{
			using (var db = new AppDb())
			{
				db.Connection.Open();
				var query = new BlogPostQuery(db);
				query.DeleteAll();
				return new OkResult();
			}
		}

		// GET api/sync/hello
		// This method is used to establish baseline web server performance
		// i.e. how many base RPS can the server handle
		[HttpGet("hello")]
		public IActionResult Hello()
		{
			return new OkObjectResult("hello world");
		}

		// GET api/sync/bulkinsert/10000
		[HttpGet("bulkinsert/{num}")]
		public IActionResult BulkInsert(int num)
		{
			using (var db = new AppDb())
			{
				var time = DateTime.Now;
				db.Connection.Open();
				var txn = db.Connection.BeginTransaction();
				try
				{
					for (var i = 0; i < num; i++)
					{
						var blogPost = new BlogPost
						{
							Db = db,
							Title = "bulk",
							Content = "bulk " + num
						};
						blogPost.Insert();
					}
				}
				catch (Exception)
				{
					txn.Rollback();
					throw;
				}
				txn.Commit();
				var timing = $"Sync: Inserted {num} records in " + (DateTime.Now - time);
				Console.WriteLine(timing);
				return new OkObjectResult(timing);
			}
		}

		// GET api/sync/bulkselect
		[HttpGet("bulkselect/{num}")]
		public IActionResult BulkSelect(int num)
		{
			using (var db = new AppDb())
			{
				var time = DateTime.Now;
				db.Connection.Open();
				var query = new BlogPostQuery(db);
				var reader = query.LatestPostsCmd(num).ExecuteReader();

				var numRead = 0;
				while (reader.Read())
				{
					var post = new BlogPost(db)
					{
						Id = reader.GetFieldValue<int>(0),
						Title = reader.GetFieldValue<string>(1),
						Content = reader.GetFieldValue<string>(2)
					};
					numRead++;
				}

				var timing = $"Sync: Read {numRead} records in " + (DateTime.Now - time);
				Console.WriteLine(timing);
				return new OkObjectResult(timing);
			}
		}
	}
}
