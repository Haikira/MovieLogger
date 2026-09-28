using Microsoft.EntityFrameworkCore;
using MovieLogger.Service.Dtos.MovieWatches;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Repositories;

namespace MovieLogger.DAL.Repositories
{
    public class MovieWatchRepository(MovieLoggerDbContext context) : Repository<MovieWatch>(context), IMovieWatchRepository
    {
        public async Task<IReadOnlyList<MovieWatch>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await Context.MovieWatches
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.DateWatched)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<MovieWatch>> GetHistoryForMovieAsync(int userId, int movieId, CancellationToken cancellationToken = default)
        {
            return await Context.MovieWatches
                .Where(w => w.UserId == userId && w.MovieId == movieId)
                .OrderByDescending(w => w.DateWatched)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<(IReadOnlyList<MyMovieAggregate> Items, int TotalCount)> SearchMyMoviesAsync(
            int userId, MyMoviesQueryDto query, CancellationToken cancellationToken = default)
        {
            var grouped =
                from w in Context.MovieWatches
                where w.UserId == userId
                group w by w.MovieId into g
                select new
                {
                    MovieId = g.Key,
                    LastWatchedAt = g.Max(x => x.DateWatched),
                    LastRating = g.OrderByDescending(x => x.DateWatched).Select(x => x.Rating).First(),
                    TimesWatched = g.Count()
                };

            var filtered =
                from g in grouped
                join m in Context.Movies on g.MovieId equals m.Id
                select new { g, m };

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search;
                filtered = filtered.Where(x => x.m.Title.Contains(search));
            }

            if (query.GenreId.HasValue)
            {
                var genreId = query.GenreId.Value;
                filtered = filtered.Where(x => x.m.Genres.Any(genre => genre.Id == genreId));
            }

            if (query.Rating.HasValue)
            {
                var rating = query.Rating.Value;
                filtered = filtered.Where(x => x.g.LastRating == rating);
            }

            filtered = query.Sort switch
            {
                MyMoviesSort.DateWatchedAsc => filtered.OrderBy(x => x.g.LastWatchedAt),
                MyMoviesSort.TitleAsc => filtered.OrderBy(x => x.m.Title),
                MyMoviesSort.TitleDesc => filtered.OrderByDescending(x => x.m.Title),
                MyMoviesSort.RatingDesc => filtered.OrderByDescending(x => x.g.LastRating),
                MyMoviesSort.RatingAsc => filtered.OrderBy(x => x.g.LastRating),
                _ => filtered.OrderByDescending(x => x.g.LastWatchedAt)
            };

            var totalCount = await filtered.CountAsync(cancellationToken);

            var page = await filtered
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(x => new { x.g.MovieId, x.g.LastWatchedAt, x.g.LastRating, x.g.TimesWatched })
                .ToListAsync(cancellationToken);

            var movieIds = page.Select(x => x.MovieId).ToList();
            var movies = await Context.Movies
                .Include(m => m.Genres)
                .Where(m => movieIds.Contains(m.Id))
                .AsNoTracking()
                .ToDictionaryAsync(m => m.Id, cancellationToken);

            var items = page
                .Select(x => new MyMovieAggregate(movies[x.MovieId], x.LastWatchedAt, x.LastRating, x.TimesWatched))
                .ToList();

            return (items, totalCount);
        }

        public async Task<DashboardStats> GetDashboardStatsAsync(int userId, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var userWatches = Context.MovieWatches.Where(w => w.UserId == userId);

            var totalLogged = await userWatches.CountAsync(cancellationToken);
            var loggedThisMonth = await userWatches.CountAsync(w => w.DateWatched >= monthStart, cancellationToken);
            var averageRating = await userWatches
                .Where(w => w.Rating != null)
                .Select(w => (double?)w.Rating)
                .AverageAsync(cancellationToken);

            var recentlyWatched = await userWatches
                .OrderByDescending(w => w.DateWatched)
                .Take(5)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return new DashboardStats(totalLogged, loggedThisMonth, averageRating, recentlyWatched);
        }

        public async Task<IReadOnlyList<GenreCount>> GetTopGenresAsync(int userId, int take, CancellationToken cancellationToken = default)
        {
            var raw = await Context.MovieWatches
                .Where(w => w.UserId == userId)
                .SelectMany(w => w.Movie.Genres, (w, genre) => new { genre.Id, genre.Name })
                .GroupBy(g => new { g.Id, g.Name })
                .Select(g => new { g.Key.Id, g.Key.Name, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(take)
                .ToListAsync(cancellationToken);

            return raw.Select(g => new GenreCount(g.Id, g.Name, g.Count)).ToList();
        }
    }
}
