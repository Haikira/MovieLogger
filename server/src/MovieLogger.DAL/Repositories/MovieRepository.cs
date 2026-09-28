using Microsoft.EntityFrameworkCore;
using MovieLogger.Service.Dtos.Movies;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Repositories;

namespace MovieLogger.DAL.Repositories
{
    public class MovieRepository(MovieLoggerDbContext context) : Repository<Movie>(context), IMovieRepository
    {
        public override async Task<IReadOnlyList<Movie>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await Context.Movies
                .Include(m => m.Genres)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public override async Task<Movie?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await Context.Movies
                .Include(m => m.Genres)
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }

        public async Task<(IReadOnlyList<Movie> Items, int TotalCount)> SearchAsync(MovieSearchQueryDto query, CancellationToken cancellationToken = default)
        {
            var movies = Context.Movies.Include(m => m.Genres).AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Title))
            {
                var title = query.Title;
                movies = movies.Where(m => m.Title.Contains(title));
            }

            if (!string.IsNullOrWhiteSpace(query.Director))
            {
                var director = query.Director;
                movies = movies.Where(m => m.Director != null && m.Director.Contains(director));
            }

            if (query.Year.HasValue)
            {
                var year = query.Year.Value;
                movies = movies.Where(m => m.ReleaseYear == year);
            }

            var totalCount = await movies.CountAsync(cancellationToken);

            var items = await movies
                .OrderBy(m => m.Title)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
    }
}
