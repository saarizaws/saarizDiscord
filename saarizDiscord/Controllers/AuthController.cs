using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using System.Security.Claims;
using saarizDiscord.Data;
using saarizDiscord.Models;
using saarizDiscord.Utility;
using saarizDiscord.Models.DTO;

namespace saarizshop.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AuthController : ControllerBase
	{
		private readonly ApplicationDbContext _db;
		private ApiResponse _response;
		private string secretKey;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly RoleManager<IdentityRole> _roleManager;
		public AuthController(ApplicationDbContext db, IConfiguration configuration, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
		{
			_db = db;
			secretKey = configuration.GetValue<string>("ApiSettings:Secret");
			_response = new ApiResponse();
			_userManager = userManager;
			_roleManager = roleManager;
		}
		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginRequestDTO loginRequest)
		{
			ApplicationUser userFromDb = _db.ApplicationUsers.FirstOrDefault(u => u.UserName.ToLower() == loginRequest.UserName.ToLower());
			if (userFromDb == null)
			{
				_response.ErrorMessages.Add("Username does not exist!");
				return NotFound();
			}
			bool isValid = await _userManager.CheckPasswordAsync(userFromDb, loginRequest.Password);
			if (!isValid)
			{
				_response.StatusCode = HttpStatusCode.BadRequest;
				_response.IsSuccess = false;
				_response.ErrorMessages.Add("Password incorrect!");
				return BadRequest(_response);
			}
			//we generate JWT
			var roles = await _userManager.GetRolesAsync(userFromDb);
			JwtSecurityTokenHandler tokenHandler = new();
			byte[] key = Encoding.ASCII.GetBytes(secretKey);

			SecurityTokenDescriptor tokenDescriptor = new()
			{
				Subject = new ClaimsIdentity(new Claim[]
				{
					new Claim("fullName",userFromDb.Name),
					new Claim("id",userFromDb.Id.ToString()),
					new Claim(ClaimTypes.Email,userFromDb.UserName.ToString()),
					new Claim(ClaimTypes.Role,roles.FirstOrDefault())
				}),
				Expires = DateTime.UtcNow.AddDays(7),
				SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
			};
			SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);

			LoginResponseDTO loginResponse = new()
			{
				Email = userFromDb.Email,
				Token = tokenHandler.WriteToken(token)
			};
			if (loginResponse.Email == null || string.IsNullOrEmpty(loginResponse.Token))
			{
				_response.StatusCode = HttpStatusCode.BadRequest;
				_response.IsSuccess = false;
				_response.ErrorMessages.Add("Username or Password incorrect!");
				return BadRequest(_response);
			}
			_response.StatusCode = HttpStatusCode.OK;
			_response.IsSuccess = true;
			_response.Result = loginResponse;
			return Ok(_response);
		}
		[HttpPost("register")]
		public async Task<IActionResult> Register([FromBody] RegisterRequestDTO registerRequest)
		{
			ApplicationUser userFromDb = _db.ApplicationUsers.FirstOrDefault(u => u.UserName.ToLower() == registerRequest.UserName.ToLower());
			if (userFromDb != null)
			{
				_response.StatusCode = HttpStatusCode.BadRequest;
				_response.IsSuccess = false;
				_response.ErrorMessages.Add("Username already exists!");
				return BadRequest(_response);
			}
			ApplicationUser newUser = new()
			{
				UserName = registerRequest.UserName,
				Email = registerRequest.UserName,
				NormalizedEmail = registerRequest.UserName.ToUpper(),
				Name = registerRequest.Name
			};
			var result = await _userManager.CreateAsync(newUser, registerRequest.Password);
			try
			{
				if (result.Succeeded)
				{
					if (!_roleManager.RoleExistsAsync(SD.Role_Admin).GetAwaiter().GetResult())
					{
						//create roles in db
						await _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin));
						await _roleManager.CreateAsync(new IdentityRole(SD.Role_Customer));
					}
					if (registerRequest.Role.ToLower() == SD.Role_Admin)
					{
						await _userManager.AddToRoleAsync(newUser, SD.Role_Admin);
					}
					else
					{
						await _userManager.AddToRoleAsync(newUser, SD.Role_Customer);
					}
					_response.StatusCode = HttpStatusCode.OK;
					_response.IsSuccess = true;
					return Ok(_response);
				}
				else
				{
					_response.StatusCode = HttpStatusCode.BadRequest;
					_response.IsSuccess = false;
					_response.ErrorMessages.Add("Error while registering");
					return BadRequest(_response);
				}
			}
			catch
			{
				_response.StatusCode = HttpStatusCode.BadRequest;
				_response.IsSuccess = false;
				_response.ErrorMessages.Add("Error while registering");
				return BadRequest(_response);
			}
		}
	}
}
