using AutoMapper;
using MovieLogger.Service.Dtos.Watchlist;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Interfaces;
using MovieLogger.Service.Repositories;

namespace MovieLogger.Service.Services
{
    public class WatchlistService(
        IWatchlistItemRepository watchlistItemRepository,
        IMovieRepository movieRepository,
        IMapper mapper) : IWatchlistService
    {
        public async Task<IReadOnlyList<WatchlistItemResponseDto>> GetMineAsync(int userId, CancellationToken cancellationToken = default)
        {
            var items = await watchlistItemRepository.GetByUserIdAsync(userId, cancellationToken);
            return items.Select(mapper.Map<WatchlistItemResponseDto>).ToList();
        }

        public async Task<AddToWatchlistResult> AddAsync(int userId, int movieId, CancellationToken cancellationToken = default)
        {
            var movie = await movieRepository.GetByIdAsync(movieId, cancellationToken);
            if (movie is null)
            {
                return AddToWatchlistResult.MovieNotFound();
            }

            var existing = await watchlistItemRepository.GetAsync(userId, movieId, cancellationToken);
            if (existing is not null)
            {
                return AddToWatchlistResult.AlreadyExists();
            }

            var watchlistItem = new WatchlistItem
            {
                UserId = userId,
                MovieId = movieId,
                DateAdded = DateTime.UtcNow
            };

            await watchlistItemRepository.AddAsync(watchlistItem, cancellationToken);
            watchlistItem.Movie = movie;

            return AddToWatchlistResult.Success(mapper.Map<WatchlistItemResponseDto>(watchlistItem));
        }

        public Task<bool> RemoveAsync(int userId, int movieId, CancellationToken cancellationToken = default)
        {
            return watchlistItemRepository.DeleteAsync(userId, movieId, cancellationToken);
        }
    }
}
