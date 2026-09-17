using MovieLogger.Service.Dtos.Movies;
using MovieLogger.Service.Services;

namespace MovieLogger.Service.Interfaces
{
    public interface IMovieService
    {
        Task<IReadOnlyList<MovieResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<MovieResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<MovieMutationResult> CreateAsync(CreateMovieDto dto, CancellationToken cancellationToken = default);

        Task<MovieMutationResult> UpdateAsync(int id, UpdateMovieDto dto, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
