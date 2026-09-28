using MovieLogger.Service.Dtos.Auth;

namespace MovieLogger.Service.Services
{
    public enum RegisterOutcome
    {
        Success,
        DuplicateEmail
    }

    public class RegisterResult
    {
        public required RegisterOutcome Outcome { get; init; }

        public AuthResponseDto? AuthResponse { get; init; }

        public static RegisterResult Success(AuthResponseDto authResponse) =>
            new() { Outcome = RegisterOutcome.Success, AuthResponse = authResponse };

        public static RegisterResult DuplicateEmail() =>
            new() { Outcome = RegisterOutcome.DuplicateEmail };
    }
}
