using Microsoft.AspNetCore.Mvc;
using MySqlConnector.Performance.Models;

namespace MySqlConnector.Performance.Controllers
{
	[Route("api/[controller]")]
	public class SyncController : Controller
	{
		// GET api/async
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

		// GET api/async/5
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

		// POST api/async
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

		// PUT api/async/5
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

		// DELETE api/async/5
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

		// DELETE api/async
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
	}
}
