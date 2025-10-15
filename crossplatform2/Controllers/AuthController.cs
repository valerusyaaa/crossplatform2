using crossplatform2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace crossplatform2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly IConfiguration _configuration;

        public AuthController(UserService userService, IConfiguration configuration)
        {
            _userService = userService;
            _configuration = configuration;
        }

        public struct LoginData
        {
            public string login { get; set; }
            public string password { get; set; }
        }

        [HttpPost("login")]
        public async Task<object> Login([FromBody] LoginData ld)
        {
            var user = await _userService.AuthenticateAsync(ld.login, ld.password);

            if (user == null)
            {
                return Unauthorized(new { message = "Wrong login/password" });
            }

            var token = AuthOptions.GenerateToken(user.Username, user.Role, _configuration);

            return new { token = token, role = user.Role };
        }

        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] RegisterRequest request)
        {
            var (success, user, error) = await _userService.CreateUserAsync(request.Username, request.Password, "User");

            if (!success)
                return BadRequest(new { error = error });

            return Ok(new { message = "User created successfully", username = user.Username });
        }

        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<UserResponse>>> GetUsers()
        {
            var users = await _userService.GetUsersAsync();
            var userResponses = users.Select(u => new UserResponse
            {
                Id = u.Id,
                Username = u.Username,
                Role = u.Role,
                CreatedAt = u.CreatedAt,
                LastLogin = u.LastLogin
            }).ToList();

            return Ok(userResponses);
        }
    }
    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UserResponse
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
    }
}