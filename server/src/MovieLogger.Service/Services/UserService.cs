using AutoMapper;
using MovieLogger.Service.Dtos.Users;
using MovieLogger.Service.Interfaces;
using MovieLogger.Service.Repositories;
using MovieLogger.Service.Security;

namespace MovieLogger.Service.Services
{
    public class UserService(IUserRepository userRepository, IPasswordHasher passwordHasher, IMapper mapper) : IUserService
    {
        public async Task<UserResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var user = await userRepository.GetByIdAsync(id, cancellationToken);
            return user is null ? null : mapper.Map<UserResponseDto>(user);
        }

        public async Task<bool> UpdateAsync(int id, UpdateUserDto dto, CancellationToken cancellationToken = default)
        {
            var user = await userRepository.GetByIdAsync(id, cancellationToken);
            if (user is null)
            {
                return false;
            }

            mapper.Map(dto, user);
            user.UpdatedAt = DateTime.UtcNow;
            await userRepository.UpdateAsync(user, cancellationToken);
            return true;
        }

        public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            return userRepository.DeleteAsync(id, cancellationToken);
        }

        public async Task<ChangePasswordResult> ChangePasswordAsync(int id, ChangePasswordDto dto, CancellationToken cancellationToken = default)
        {
            var user = await userRepository.GetByIdAsync(id, cancellationToken);
            if (user is null)
            {
                return ChangePasswordResult.UserNotFound();
            }

            if (!passwordHasher.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
            {
                return ChangePasswordResult.IncorrectCurrentPassword();
            }

            user.PasswordHash = passwordHasher.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await userRepository.UpdateAsync(user, cancellationToken);
            return ChangePasswordResult.Success();
        }
    }
}
