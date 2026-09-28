using MovieLogger.Service.Entities;

namespace MovieLogger.Service.Security
{
    public interface IJwtTokenGenerator
    {
        (string Token, DateTime ExpiresAt) GenerateToken(User user);
    }
}
