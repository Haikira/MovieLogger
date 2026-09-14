using MovieLogger.Service.Entities;

namespace MovieLogger.Service.Repositories
{
    public interface IListMovieRepository
    {
        Task<ListMovie?> GetAsync(int listId, int movieId, CancellationToken cancellationToken = default);

        Task AddAsync(ListMovie listMovie, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int listId, int movieId, CancellationToken cancellationToken = default);
    }
}
