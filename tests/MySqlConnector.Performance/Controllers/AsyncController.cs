using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector.Performance.Models;

namespace MySqlConnector.Performance.Controllers
{
	[Route("api/[controller]")]
	public class AsyncController : Controller
	{
		// GET api/async
		[HttpGet]
		public async Task<IActionResult> GetLatest()
		{
			using (var db = new AppDb())
			{
				await db.Connection.OpenAsync();
				var query = new BlogPostQuery(db);
				var result = await query.LatestPostsAsync();
				return new OkObjectResult(result);
			}
		}

		// GET api/async/5
		[HttpGet("{id}")]
		public async Task<IActionResult> GetOne(int id)
		{
			using (var db = new AppDb())
			{
				await db.Connection.OpenAsync();
				var query = new BlogPostQuery(db);
				var result = await query.FindOneAsync(id);
				if (result == null)
					return new NotFoundResult();
				return new OkObjectResult(result);
			}
		}

		// POST api/async
		[HttpPost]
		public async Task<IActionResult> Post([FromBody]BlogPost body)
		{
			using (var db = new AppDb())
			{
				await db.Connection.OpenAsync();
				body.Db = db;
				await body.InsertAsync();
				return new OkObjectResult(body);
			}
		}

		// PUT api/async/5
		[HttpPut("{id}")]
		public async Task<IActionResult> PutOne(int id, [FromBody]BlogPost body)
		{
			using (var db = new AppDb())
			{
				await db.Connection.OpenAsync();
				var query = new BlogPostQuery(db);
				var result = await query.FindOneAsync(id);
				if (result == null)
					return new NotFoundResult();
				result.Title = body.Title;
				result.Content = body.Content;
				await result.UpdateAsync();
				return new OkObjectResult(result);
			}
		}

		// DELETE api/async/5
		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteOne(int id)
		{
			using (var db = new AppDb())
			{
				await db.Connection.OpenAsync();
				var query = new BlogPostQuery(db);
				var result = await query.FindOneAsync(id);
				if (result == null)
					return new NotFoundResult();
				await result.DeleteAsync();
				return new OkResult();
			}
		}

		// DELETE api/async
		[HttpDelete]
		public async Task<IActionResult> DeleteAll()
		{
			using (var db = new AppDb())
			{
				await db.Connection.OpenAsync();
				var query = new BlogPostQuery(db);
				await query.DeleteAllAsync();
				return new OkResult();
			}
		}

		// GET api/async/bulkinsert/10000
		[HttpGet("bulkinsert/{num}")]
		public async Task<IActionResult> BulkInsert(int num)
		{
			using (var db = new AppDb())
			{
				var time = DateTime.Now;
				await db.Connection.OpenAsync();
				var txn = await db.Connection.BeginTransactionAsync();
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
						await blogPost.InsertAsync();
					}
#if BASELINE
					txn.Commit();
#else
					await txn.CommitAsync();
#endif
				}
				catch (Exception)
				{
#if BASELINE
					txn.Rollback();
#else
					await txn.RollbackAsync();
#endif
					throw;
				}
				var timing = $"Async: Inserted {num} records in " + (DateTime.Now - time);
				Console.WriteLine(timing);
				return new OkObjectResult(timing);
			}
		}

		// GET api/async/bulkselect
		[HttpGet("bulkselect/{num}")]
		public async Task<IActionResult> BulkSelect(int num)
		{
			using (var db = new AppDb())
			{
				var time = DateTime.Now;
				await db.Connection.OpenAsync();
				var query = new BlogPostQuery(db);
				var reader = await query.LatestPostsCmd(num).ExecuteReaderAsync();

				var numRead = 0;
				while (await reader.ReadAsync())
				{
					var post = new BlogPost(db)
					{
						Id = await reader.GetFieldValueAsync<int>(0),
						Title = await reader.GetFieldValueAsync<string>(1),
						Content = await reader.GetFieldValueAsync<string>(2)
					};
					numRead++;
				}

				var timing = $"Async: Read {numRead} records in " + (DateTime.Now - time);
				Console.WriteLine(timing);
				return new OkObjectResult(timing);
			}
		}
	}
}
