namespace MovieLogger.Service.Services
{
    public enum UpdateUserOutcome
    {
        Success,
        UserNotFound,
        DuplicateEmail
    }

    public class UpdateUserResult
    {
        public required UpdateUserOutcome Outcome { get; init; }

        public static UpdateUserResult Success() =>
            new() { Outcome = UpdateUserOutcome.Success };

        public static UpdateUserResult UserNotFound() =>
            new() { Outcome = UpdateUserOutcome.UserNotFound };

        public static UpdateUserResult DuplicateEmail() =>
            new() { Outcome = UpdateUserOutcome.DuplicateEmail };
    }
}
