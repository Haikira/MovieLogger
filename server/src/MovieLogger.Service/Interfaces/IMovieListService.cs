using MovieLogger.Service.Dtos.Lists;
using MovieLogger.Service.Services;

namespace MovieLogger.Service.Interfaces
{
    public interface IMovieListService
    {
        Task<IReadOnlyList<MovieListResponseDto>> GetMineAsync(int userId, CancellationToken cancellationToken = default);

        Task<MovieListResponseDto?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default);

        Task<MovieListMutationResult> CreateAsync(CreateMovieListDto dto, int userId, CancellationToken cancellationToken = default);

        Task<bool> UpdateAsync(int id, UpdateMovieListDto dto, int userId, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, int userId, CancellationToken cancellationToken = default);
    }
}
