using MovieLogger.Service.Entities;

namespace MovieLogger.Service.Repositories
{
    public interface IWatchlistItemRepository
    {
        Task<WatchlistItem?> GetAsync(int userId, int movieId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<WatchlistItem>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

        Task<int> CountByUserIdAsync(int userId, CancellationToken cancellationToken = default);

        Task AddAsync(WatchlistItem watchlistItem, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int userId, int movieId, CancellationToken cancellationToken = default);
    }
}
