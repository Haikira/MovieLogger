using MovieLogger.Service.Dtos.Common;
using MovieLogger.Service.Dtos.MovieWatches;
using MovieLogger.Service.Services;

namespace MovieLogger.Service.Interfaces
{
    public interface IMovieWatchService
    {
        Task<IReadOnlyList<MovieWatchResponseDto>> GetMineAsync(int userId, CancellationToken cancellationToken = default);

        Task<MovieWatchResponseDto?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default);

        Task<MovieWatchMutationResult> CreateAsync(CreateMovieWatchDto dto, int userId, CancellationToken cancellationToken = default);

        Task<bool> UpdateAsync(int id, UpdateMovieWatchDto dto, int userId, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, int userId, CancellationToken cancellationToken = default);

        Task<PagedResult<MyMovieResponseDto>> GetMyMoviesAsync(int userId, MyMoviesQueryDto query, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<MovieWatchResponseDto>> GetHistoryForMovieAsync(int movieId, int userId, CancellationToken cancellationToken = default);
    }
}
