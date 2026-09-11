using MovieLogger.Service.Entities;

namespace MovieLogger.Service.Repositories
{
    public interface IGenreRepository : IRepository<Genre>
    {
        Task<IReadOnlyList<Genre>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
    }
}
