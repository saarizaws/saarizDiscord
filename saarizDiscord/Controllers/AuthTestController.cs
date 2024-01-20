using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using saarizDiscord.Utility;

namespace saarizDiscord.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AuthTestController : ControllerBase
	{
		[HttpGet]
		[Authorize]
		public async Task<ActionResult<string>> GetSomething()
		{
			return "You are authenticated";
		}
		[HttpGet("{id:int}")]
		[Authorize(Roles = SD.Role_Admin)]
		public async Task<ActionResult<string>> GetSomething(int someIntValue)
		{
			return "You are authorized as Admin";
		}
	}
}
