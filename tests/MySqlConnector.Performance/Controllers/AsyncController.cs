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
        public async Task<IActionResult> Get()
        {
            using (var db = new AppDb())
            {
                await db.OpenAsync();
                var cmd = BlogPost.LatestPosts(db);
                var reader = await cmd.ExecuteReaderAsync();
                var result = await BlogPost.ReadAllAsync(reader);
                return new OkObjectResult(result);
            }
        }

        // POST api/async
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]BlogPost body)
        {
            using (var db = new AppDb())
            {
                await db.OpenAsync();
                await BlogPost.InsertAsync(db, body);
                return new OkObjectResult(body);
            }
        }
    }
}
