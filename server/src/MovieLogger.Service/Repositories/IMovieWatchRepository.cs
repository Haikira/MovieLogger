using MovieLogger.Service.Dtos.MovieWatches;
using MovieLogger.Service.Entities;

namespace MovieLogger.Service.Repositories
{
    public record MyMovieAggregate(Movie Movie, DateTime LastWatchedAt, int? LastRating, int TimesWatched);

    public record DashboardStats(
        int TotalLogged,
        int LoggedThisMonth,
        double? AverageRating,
        IReadOnlyList<MovieWatch> RecentlyWatched);

    public record GenreCount(int GenreId, string GenreName, int Count);

    public interface IMovieWatchRepository : IRepository<MovieWatch>
    {
        Task<IReadOnlyList<MovieWatch>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<MovieWatch>> GetHistoryForMovieAsync(int userId, int movieId, CancellationToken cancellationToken = default);

        Task<(IReadOnlyList<MyMovieAggregate> Items, int TotalCount)> SearchMyMoviesAsync(
            int userId, MyMoviesQueryDto query, CancellationToken cancellationToken = default);

        Task<DashboardStats> GetDashboardStatsAsync(int userId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<GenreCount>> GetTopGenresAsync(int userId, int take, CancellationToken cancellationToken = default);
    }
}
