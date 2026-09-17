using MovieLogger.Service.Dtos.MovieWatches;
using MovieLogger.Service.Services;

namespace MovieLogger.Service.Interfaces
{
    public interface IMovieWatchService
    {
        Task<IReadOnlyList<MovieWatchResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<MovieWatchResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<MovieWatchMutationResult> CreateAsync(CreateMovieWatchDto dto, CancellationToken cancellationToken = default);

        Task<bool> UpdateAsync(int id, UpdateMovieWatchDto dto, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
