using MovieLogger.Service.Dtos.Lists;
using MovieLogger.Service.Services;

namespace MovieLogger.Service.Interfaces
{
    public interface IMovieListService
    {
        Task<IReadOnlyList<MovieListResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<MovieListResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<MovieListMutationResult> CreateAsync(CreateMovieListDto dto, CancellationToken cancellationToken = default);

        Task<bool> UpdateAsync(int id, UpdateMovieListDto dto, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
