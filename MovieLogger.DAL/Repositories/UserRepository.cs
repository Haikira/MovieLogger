using MovieLogger.Service.Entities;
using MovieLogger.Service.Repositories;

namespace MovieLogger.DAL.Repositories
{
    public class UserRepository(MovieLoggerDbContext context) : Repository<User>(context), IUserRepository
    {
    }
}
