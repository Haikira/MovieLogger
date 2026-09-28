using AutoMapper;
using MovieLogger.Service.Dtos.Dashboard;
using MovieLogger.Service.Dtos.MovieWatches;
using MovieLogger.Service.Interfaces;
using MovieLogger.Service.Repositories;

namespace MovieLogger.Service.Services
{
    public class DashboardService(
        IMovieWatchRepository movieWatchRepository,
        IWatchlistItemRepository watchlistItemRepository,
        IMapper mapper) : IDashboardService
    {
        private const int TopGenresCount = 5;

        public async Task<DashboardResponseDto> GetDashboardAsync(int userId, CancellationToken cancellationToken = default)
        {
            var stats = await movieWatchRepository.GetDashboardStatsAsync(userId, cancellationToken);
            var watchlistCount = await watchlistItemRepository.CountByUserIdAsync(userId, cancellationToken);
            var topGenres = await movieWatchRepository.GetTopGenresAsync(userId, TopGenresCount, cancellationToken);

            return new DashboardResponseDto
            {
                TotalMoviesLogged = stats.TotalLogged,
                MoviesWatchedThisMonth = stats.LoggedThisMonth,
                AverageRating = stats.AverageRating,
                WatchlistCount = watchlistCount,
                RecentlyWatched = stats.RecentlyWatched.Select(mapper.Map<MovieWatchResponseDto>).ToList(),
                TopGenres = topGenres.Select(g => new GenreCountDto
                {
                    GenreId = g.GenreId,
                    GenreName = g.GenreName,
                    Count = g.Count
                }).ToList()
            };
        }
    }
}
