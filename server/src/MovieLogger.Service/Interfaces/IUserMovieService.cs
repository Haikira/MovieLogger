using MovieLogger.Service.Dtos.Users;
using MovieLogger.Service.Services;

namespace MovieLogger.Service.Interfaces
{
    public interface IUserMovieService
    {
        Task<IReadOnlyList<UserMovieResponseDto>?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

        Task<SetUserMovieStatusResult> SetStatusAsync(int userId, int movieId, SetUserMovieStatusDto dto, CancellationToken cancellationToken = default);

        Task<bool> RemoveAsync(int userId, int movieId, CancellationToken cancellationToken = default);
    }
}
