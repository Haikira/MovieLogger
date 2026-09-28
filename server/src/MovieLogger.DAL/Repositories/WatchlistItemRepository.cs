using Microsoft.EntityFrameworkCore;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Repositories;

namespace MovieLogger.DAL.Repositories
{
    public class WatchlistItemRepository(MovieLoggerDbContext context) : IWatchlistItemRepository
    {
        public async Task<WatchlistItem?> GetAsync(int userId, int movieId, CancellationToken cancellationToken = default)
        {
            return await context.WatchlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId && w.MovieId == movieId, cancellationToken);
        }

        public async Task<IReadOnlyList<WatchlistItem>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await context.WatchlistItems
                .Include(w => w.Movie)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.DateAdded)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public Task<int> CountByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return context.WatchlistItems.CountAsync(w => w.UserId == userId, cancellationToken);
        }

        public async Task AddAsync(WatchlistItem watchlistItem, CancellationToken cancellationToken = default)
        {
            context.WatchlistItems.Add(watchlistItem);
            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> DeleteAsync(int userId, int movieId, CancellationToken cancellationToken = default)
        {
            var existing = await context.WatchlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId && w.MovieId == movieId, cancellationToken);

            if (existing is null)
            {
                return false;
            }

            context.WatchlistItems.Remove(existing);
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
