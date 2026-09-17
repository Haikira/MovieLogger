using MovieLogger.Service.Dtos.MovieWatches;

namespace MovieLogger.Service.Services
{
    public enum MovieWatchMutationOutcome
    {
        Success,
        InvalidUserId,
        InvalidMovieId
    }

    public class MovieWatchMutationResult
    {
        public required MovieWatchMutationOutcome Outcome { get; init; }

        public MovieWatchResponseDto? MovieWatch { get; init; }

        public static MovieWatchMutationResult Success(MovieWatchResponseDto movieWatch) =>
            new() { Outcome = MovieWatchMutationOutcome.Success, MovieWatch = movieWatch };

        public static MovieWatchMutationResult InvalidUser() =>
            new() { Outcome = MovieWatchMutationOutcome.InvalidUserId };

        public static MovieWatchMutationResult InvalidMovie() =>
            new() { Outcome = MovieWatchMutationOutcome.InvalidMovieId };
    }
}
