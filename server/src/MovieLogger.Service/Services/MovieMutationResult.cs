using MovieLogger.Service.Dtos.Movies;

namespace MovieLogger.Service.Services
{
    public enum MovieMutationOutcome
    {
        Success,
        NotFound,
        InvalidGenreIds,
        /// <summary>The movie exists but was added by another user, who alone may change or delete it.</summary>
        NotCreator
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

        public static MovieMutationResult NotCreator() =>
            new() { Outcome = MovieMutationOutcome.NotCreator };

        public static MovieMutationResult InvalidGenres(IReadOnlyList<int> invalidGenreIds) =>
            new() { Outcome = MovieMutationOutcome.InvalidGenreIds, InvalidGenreIds = invalidGenreIds };
    }
}
