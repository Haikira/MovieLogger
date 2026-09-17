namespace MovieLogger.Service.Services
{
    public enum SetUserMovieStatusOutcome
    {
        Success,
        UserNotFound,
        MovieNotFound
    }

    public class SetUserMovieStatusResult
    {
        public required SetUserMovieStatusOutcome Outcome { get; init; }

        public static SetUserMovieStatusResult Success() =>
            new() { Outcome = SetUserMovieStatusOutcome.Success };

        public static SetUserMovieStatusResult UserNotFound() =>
            new() { Outcome = SetUserMovieStatusOutcome.UserNotFound };

        public static SetUserMovieStatusResult MovieNotFound() =>
            new() { Outcome = SetUserMovieStatusOutcome.MovieNotFound };
    }
}
