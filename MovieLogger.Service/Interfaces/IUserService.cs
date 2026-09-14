using MovieLogger.Service.Dtos.Users;

namespace MovieLogger.Service.Interfaces
{
    public interface IUserService
    {
        Task<IReadOnlyList<UserResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<UserResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<UserResponseDto> CreateAsync(CreateUserDto dto, CancellationToken cancellationToken = default);

        Task<bool> UpdateAsync(int id, UpdateUserDto dto, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
