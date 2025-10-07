using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using crossplatform2.Models;
using crossplatform2.Data;

namespace crossplatform2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public struct LoginData
        {
            public string login { get; set; }
            public string password { get; set; }
        }

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<object> Login([FromBody] LoginData ld)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == ld.login);

            if (user == null || !user.CheckPassword(ld.password))
            {
                Response.StatusCode = 401;
                return new { message = "Wrong login/password" };
            }

            var token = AuthOptions.GenerateToken(user.Username, user.Role);
            return new { token = token, role = user.Role };
        }

        [HttpGet("users")]
        public async Task<ActionResult<List<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        [HttpGet("token")]
        public object GetToken()
        {
            return new { token = AuthOptions.GenerateToken("user", "User") };
        }

        [HttpGet("token/secret")]
        public object GetAdminToken()
        {
            return new { token = AuthOptions.GenerateToken("admin", "Admin") };
        }

        [HttpGet("profile")]
        public object GetProfile()
        {
            // Этот метод может использоваться для проверки текущего пользователя
            return new { message = "Profile endpoint - requires authentication" };
        }
    }
}