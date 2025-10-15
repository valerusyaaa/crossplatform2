using crossplatform2.Data;

namespace crossplatform2.Services
{
    public class DbInitializerService
    {
        private readonly ApplicationDbContext _context;

        public DbInitializerService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task InitializeAsync()
        {
            await _context.Database.CanConnectAsync();
        }
    }
}