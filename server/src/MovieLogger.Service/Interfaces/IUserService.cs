using MovieLogger.Service.Dtos.Users;
using MovieLogger.Service.Services;

namespace MovieLogger.Service.Interfaces
{
    public interface IUserService
    {
        Task<UserResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<bool> UpdateAsync(int id, UpdateUserDto dto, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

        Task<ChangePasswordResult> ChangePasswordAsync(int id, ChangePasswordDto dto, CancellationToken cancellationToken = default);
    }
}
