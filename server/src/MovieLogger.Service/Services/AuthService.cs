using AutoMapper;
using MovieLogger.Service.Dtos.Auth;
using MovieLogger.Service.Dtos.Users;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Interfaces;
using MovieLogger.Service.Repositories;
using MovieLogger.Service.Security;

namespace MovieLogger.Service.Services
{
    public class AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IMapper mapper) : IAuthService
    {
        public async Task<RegisterResult> RegisterAsync(RegisterRequestDto dto, CancellationToken cancellationToken = default)
        {
            var existing = await userRepository.GetByEmailAsync(dto.Email, cancellationToken);
            if (existing is not null)
            {
                return RegisterResult.DuplicateEmail();
            }

            var user = new User
            {
                DisplayName = dto.DisplayName,
                Email = dto.Email,
                PasswordHash = passwordHasher.HashPassword(dto.Password),
                CreatedAt = DateTime.UtcNow
            };

            await userRepository.AddAsync(user, cancellationToken);

            return RegisterResult.Success(BuildAuthResponse(user));
        }

        public async Task<LoginResult> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default)
        {
            var user = await userRepository.GetByEmailAsync(dto.Email, cancellationToken);
            if (user is null || !passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
            {
                return LoginResult.InvalidCredentials();
            }

            return LoginResult.Success(BuildAuthResponse(user));
        }

        private AuthResponseDto BuildAuthResponse(User user)
        {
            var (token, expiresAt) = jwtTokenGenerator.GenerateToken(user);

            return new AuthResponseDto
            {
                Token = token,
                ExpiresAt = expiresAt,
                User = mapper.Map<UserResponseDto>(user)
            };
        }
    }
}
