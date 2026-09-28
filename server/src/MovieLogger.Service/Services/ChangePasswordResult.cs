namespace MovieLogger.Service.Services
{
    public enum ChangePasswordOutcome
    {
        Success,
        UserNotFound,
        IncorrectCurrentPassword
    }

    public class ChangePasswordResult
    {
        public required ChangePasswordOutcome Outcome { get; init; }

        public static ChangePasswordResult Success() =>
            new() { Outcome = ChangePasswordOutcome.Success };

        public static ChangePasswordResult UserNotFound() =>
            new() { Outcome = ChangePasswordOutcome.UserNotFound };

        public static ChangePasswordResult IncorrectCurrentPassword() =>
            new() { Outcome = ChangePasswordOutcome.IncorrectCurrentPassword };
    }
}
