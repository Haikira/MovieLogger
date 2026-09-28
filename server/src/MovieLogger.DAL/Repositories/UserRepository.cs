using Microsoft.EntityFrameworkCore;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Repositories;

namespace MovieLogger.DAL.Repositories
{
    public class UserRepository(MovieLoggerDbContext context) : Repository<User>(context), IUserRepository
    {
        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await Context.Users
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        }
    }
}
