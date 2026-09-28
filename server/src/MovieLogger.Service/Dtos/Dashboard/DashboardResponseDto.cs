using MovieLogger.Service.Dtos.MovieWatches;

namespace MovieLogger.Service.Dtos.Dashboard
{
    public class DashboardResponseDto
    {
        public int TotalMoviesLogged { get; set; }

        public int MoviesWatchedThisMonth { get; set; }

        public double? AverageRating { get; set; }

        public int WatchlistCount { get; set; }

        public List<MovieWatchResponseDto> RecentlyWatched { get; set; } = [];

        public List<GenreCountDto> TopGenres { get; set; } = [];
    }

    public class GenreCountDto
    {
        public int GenreId { get; set; }

        public string GenreName { get; set; } = string.Empty;

        public int Count { get; set; }
    }
}
