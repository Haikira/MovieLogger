using MovieLogger.Service.Dtos.Common;
using MovieLogger.Service.Dtos.Movies;
using MovieLogger.Service.Services;

namespace MovieLogger.Service.Interfaces
{
    public interface IMovieService
    {
        Task<PagedResult<MovieResponseDto>> SearchAsync(MovieSearchQueryDto query, CancellationToken cancellationToken = default);

        Task<MovieDetailsResponseDto?> GetDetailsAsync(int id, int? currentUserId, CancellationToken cancellationToken = default);

        Task<MovieMutationResult> CreateAsync(CreateMovieDto dto, int createdByUserId, CancellationToken cancellationToken = default);

        Task<MovieMutationResult> UpdateAsync(int id, UpdateMovieDto dto, int currentUserId, CancellationToken cancellationToken = default);

        Task<MovieMutationOutcome> DeleteAsync(int id, int currentUserId, CancellationToken cancellationToken = default);
    }
}
