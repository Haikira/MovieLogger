using Microsoft.EntityFrameworkCore;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Repositories;

namespace MovieLogger.DAL.Repositories
{
    public class MovieListRepository(MovieLoggerDbContext context) : Repository<MovieList>(context), IMovieListRepository
    {
        public async Task<IReadOnlyList<MovieList>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await Context.Lists
                .Where(l => l.UserId == userId)
                .Include(l => l.ListMovies)
                    .ThenInclude(lm => lm.Movie)
                        .ThenInclude(m => m.Genres)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public override async Task<MovieList?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await Context.Lists
                .Include(l => l.ListMovies)
                    .ThenInclude(lm => lm.Movie)
                        .ThenInclude(m => m.Genres)
                .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        }
    }
}
