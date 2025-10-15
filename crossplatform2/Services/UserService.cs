using Microsoft.EntityFrameworkCore;
using crossplatform2.Data;
using crossplatform2.Models;

namespace crossplatform2.Services
{
    public class UserService
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordService _passwordService;

        public UserService(ApplicationDbContext context, PasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null || !_passwordService.VerifyPassword(password, user.PasswordHash))
                return null;

            // Обновляем время последнего входа
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<(bool success, User? user, string? error)> CreateUserAsync(string username, string password, string role)
        {
            if (await _context.Users.AnyAsync(u => u.Username == username))
                return (false, null, "User already exists");

            var user = new User
            {
                Username = username,
                PasswordHash = _passwordService.HashPassword(password),
                Role = role,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return (true, user, null);
        }

        public async Task<bool> UserExistsAsync(string username)
        {
            return await _context.Users.AnyAsync(u => u.Username == username);
        }

        public async Task<List<User>> GetUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }
    }
}