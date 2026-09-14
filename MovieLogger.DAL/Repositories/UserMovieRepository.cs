using Microsoft.EntityFrameworkCore;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Repositories;

namespace MovieLogger.DAL.Repositories
{
    public class UserMovieRepository(MovieLoggerDbContext context) : IUserMovieRepository
    {
        public async Task<UserMovie?> GetAsync(int userId, int movieId, CancellationToken cancellationToken = default)
        {
            return await context.Set<UserMovie>()
                .FirstOrDefaultAsync(um => um.UserId == userId && um.MovieId == movieId, cancellationToken);
        }

        public async Task<IReadOnlyList<UserMovie>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await context.Set<UserMovie>()
                .Include(um => um.Movie)
                    .ThenInclude(m => m.Genres)
                .Where(um => um.UserId == userId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task UpsertAsync(UserMovie userMovie, CancellationToken cancellationToken = default)
        {
            var existing = await context.Set<UserMovie>()
                .FirstOrDefaultAsync(um => um.UserId == userMovie.UserId && um.MovieId == userMovie.MovieId, cancellationToken);

            if (existing is null)
            {
                context.Set<UserMovie>().Add(userMovie);
            }
            else
            {
                existing.IsFavourite = userMovie.IsFavourite;
                existing.IsOwned = userMovie.IsOwned;
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> DeleteAsync(int userId, int movieId, CancellationToken cancellationToken = default)
        {
            var existing = await context.Set<UserMovie>()
                .FirstOrDefaultAsync(um => um.UserId == userId && um.MovieId == movieId, cancellationToken);

            if (existing is null)
            {
                return false;
            }

            context.Set<UserMovie>().Remove(existing);
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
