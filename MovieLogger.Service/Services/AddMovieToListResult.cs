namespace MovieLogger.Service.Services
{
    public enum AddMovieToListOutcome
    {
        Success,
        ListNotFound,
        MovieNotFound
    }

    public class AddMovieToListResult
    {
        public required AddMovieToListOutcome Outcome { get; init; }

        public static AddMovieToListResult Success() =>
            new() { Outcome = AddMovieToListOutcome.Success };

        public static AddMovieToListResult ListNotFound() =>
            new() { Outcome = AddMovieToListOutcome.ListNotFound };

        public static AddMovieToListResult MovieNotFound() =>
            new() { Outcome = AddMovieToListOutcome.MovieNotFound };
    }
}
