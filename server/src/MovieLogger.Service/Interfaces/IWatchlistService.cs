using MovieLogger.Service.Dtos.Watchlist;
using MovieLogger.Service.Services;

namespace MovieLogger.Service.Interfaces
{
    public interface IWatchlistService
    {
        Task<IReadOnlyList<WatchlistItemResponseDto>> GetMineAsync(int userId, CancellationToken cancellationToken = default);

        Task<AddToWatchlistResult> AddAsync(int userId, int movieId, CancellationToken cancellationToken = default);

        Task<bool> RemoveAsync(int userId, int movieId, CancellationToken cancellationToken = default);
    }
}
