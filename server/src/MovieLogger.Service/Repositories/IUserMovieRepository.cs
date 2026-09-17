using MovieLogger.Service.Entities;

namespace MovieLogger.Service.Repositories
{
    public interface IUserMovieRepository
    {
        Task<UserMovie?> GetAsync(int userId, int movieId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<UserMovie>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

        Task UpsertAsync(UserMovie userMovie, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int userId, int movieId, CancellationToken cancellationToken = default);
    }
}
