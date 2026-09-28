using MovieLogger.Service.Dtos.Users;

namespace MovieLogger.Service.Dtos.Auth
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public UserResponseDto User { get; set; } = null!;
    }
}
