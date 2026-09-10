using Microsoft.EntityFrameworkCore;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Repositories;

namespace MovieLogger.DAL.Repositories
{
    public class MovieRepository(MovieLoggerDbContext context) : IMovieRepository
    {
        public async Task<IReadOnlyList<Movie>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await context.Movies
                .Include(m => m.Genres)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<Movie?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await context.Movies
                .Include(m => m.Genres)
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }

        public async Task AddAsync(Movie movie, CancellationToken cancellationToken = default)
        {
            context.Movies.Add(movie);
            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Movie movie, CancellationToken cancellationToken = default)
        {
            // movie is expected to be the tracked instance returned by GetByIdAsync, mutated in place
            // (including its Genres collection), so persisting is just a SaveChanges.
            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var movie = await context.Movies.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
            if (movie is null)
            {
                return false;
            }

            context.Movies.Remove(movie);
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
