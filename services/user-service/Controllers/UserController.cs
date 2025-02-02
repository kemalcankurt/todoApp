using Microsoft.AspNetCore.Mvc;

namespace user_service.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<string>> hello()
        {
            return Ok("hello world!");
        }
    }
}