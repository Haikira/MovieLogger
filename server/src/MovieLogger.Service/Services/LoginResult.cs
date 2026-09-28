using MovieLogger.Service.Dtos.Auth;

namespace MovieLogger.Service.Services
{
    public enum LoginOutcome
    {
        Success,
        InvalidCredentials
    }

    public class LoginResult
    {
        public required LoginOutcome Outcome { get; init; }

        public AuthResponseDto? AuthResponse { get; init; }

        public static LoginResult Success(AuthResponseDto authResponse) =>
            new() { Outcome = LoginOutcome.Success, AuthResponse = authResponse };

        public static LoginResult InvalidCredentials() =>
            new() { Outcome = LoginOutcome.InvalidCredentials };
    }
}
