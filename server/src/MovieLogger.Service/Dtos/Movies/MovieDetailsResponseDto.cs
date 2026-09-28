using MovieLogger.Service.Dtos.MovieWatches;

namespace MovieLogger.Service.Dtos.Movies
{
    public class MovieDetailsResponseDto
    {
        public MovieResponseDto Movie { get; set; } = null!;

        public UserMovieHistoryDto? UserHistory { get; set; }
    }

    public class UserMovieHistoryDto
    {
        public int TimesWatched { get; set; }

        public DateTime? LastWatchedAt { get; set; }

        public int? LastRating { get; set; }

        public List<MovieWatchResponseDto> Logs { get; set; } = [];
    }
}
