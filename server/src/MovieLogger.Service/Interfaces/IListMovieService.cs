using MovieLogger.Service.Services;

namespace MovieLogger.Service.Interfaces
{
    public interface IListMovieService
    {
        Task<AddMovieToListResult> AddAsync(int listId, int movieId, int userId, CancellationToken cancellationToken = default);

        Task<bool> RemoveAsync(int listId, int movieId, int userId, CancellationToken cancellationToken = default);
    }
}
