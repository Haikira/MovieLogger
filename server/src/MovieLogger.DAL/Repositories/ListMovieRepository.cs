using Microsoft.EntityFrameworkCore;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Repositories;

namespace MovieLogger.DAL.Repositories
{
    public class ListMovieRepository(MovieLoggerDbContext context) : IListMovieRepository
    {
        public async Task<ListMovie?> GetAsync(int listId, int movieId, CancellationToken cancellationToken = default)
        {
            return await context.Set<ListMovie>()
                .FirstOrDefaultAsync(lm => lm.ListId == listId && lm.MovieId == movieId, cancellationToken);
        }

        public async Task AddAsync(ListMovie listMovie, CancellationToken cancellationToken = default)
        {
            context.Set<ListMovie>().Add(listMovie);
            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> DeleteAsync(int listId, int movieId, CancellationToken cancellationToken = default)
        {
            var existing = await context.Set<ListMovie>()
                .FirstOrDefaultAsync(lm => lm.ListId == listId && lm.MovieId == movieId, cancellationToken);

            if (existing is null)
            {
                return false;
            }

            context.Set<ListMovie>().Remove(existing);
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
