using Microsoft.EntityFrameworkCore;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Repositories;

namespace MovieLogger.DAL.Repositories
{
    public class MovieRepository(MovieLoggerDbContext context) : Repository<Movie>(context), IMovieRepository
    {
        public override async Task<IReadOnlyList<Movie>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await Context.Movies
                .Include(m => m.Genres)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public override async Task<Movie?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await Context.Movies
                .Include(m => m.Genres)
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }
    }
}
