using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector.Performance.Models;

namespace MySqlConnector.Performance.Controllers
{
    [Route("api/[controller]")]
    public class SyncController : Controller
    {
        // GET api/sync
        [HttpGet]
        public IActionResult Get()
        {
            using (var db = new AppDb())
            {
                db.Open();
                var cmd = BlogPost.LatestPosts(db);
                var reader = cmd.ExecuteReader();
                var result = BlogPost.ReadAll(reader);
                return new OkObjectResult(result);
            }
        }

        // POST api/sync
        [HttpPost]
        public IActionResult Post([FromBody] BlogPost body)
        {
            using (var db = new AppDb())
            {
                db.Open();
                BlogPost.Insert(db, body);
                return new OkObjectResult(body);
            }
        }
    }
}
