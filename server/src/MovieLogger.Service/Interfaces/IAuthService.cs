using MovieLogger.Service.Dtos.Auth;
using MovieLogger.Service.Services;

namespace MovieLogger.Service.Interfaces
{
    public interface IAuthService
    {
        Task<RegisterResult> RegisterAsync(RegisterRequestDto dto, CancellationToken cancellationToken = default);

        Task<LoginResult> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default);
    }
}
