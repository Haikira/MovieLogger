using MovieLogger.Service.Entities;
using MovieLogger.Service.Repositories;

namespace MovieLogger.DAL.Repositories
{
    public class MovieWatchRepository(MovieLoggerDbContext context) : Repository<MovieWatch>(context), IMovieWatchRepository
    {
    }
}
