using MovieLogger.Service.Dtos.Movies;

namespace MovieLogger.Service.Services
{
    public enum MovieMutationOutcome
    {
        Success,
        NotFound,
        InvalidGenreIds
    }

    public class MovieMutationResult
    {
        public required MovieMutationOutcome Outcome { get; init; }

        public MovieResponseDto? Movie { get; init; }

        public IReadOnlyList<int> InvalidGenreIds { get; init; } = [];

        public static MovieMutationResult Success(MovieResponseDto movie) =>
            new() { Outcome = MovieMutationOutcome.Success, Movie = movie };

        public static MovieMutationResult NotFound() =>
            new() { Outcome = MovieMutationOutcome.NotFound };

        public static MovieMutationResult InvalidGenres(IReadOnlyList<int> invalidGenreIds) =>
            new() { Outcome = MovieMutationOutcome.InvalidGenreIds, InvalidGenreIds = invalidGenreIds };
    }
}
