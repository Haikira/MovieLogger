using MovieLogger.Service.Dtos.Lists;

namespace MovieLogger.Service.Services
{
    public enum MovieListMutationOutcome
    {
        Success,
        InvalidUserId
    }

    public class MovieListMutationResult
    {
        public required MovieListMutationOutcome Outcome { get; init; }

        public MovieListResponseDto? MovieList { get; init; }

        public static MovieListMutationResult Success(MovieListResponseDto movieList) =>
            new() { Outcome = MovieListMutationOutcome.Success, MovieList = movieList };

        public static MovieListMutationResult InvalidUser() =>
            new() { Outcome = MovieListMutationOutcome.InvalidUserId };
    }
}
