using AutoMapper;
using MovieLogger.Service.Dtos.Common;
using MovieLogger.Service.Dtos.MovieWatches;
using MovieLogger.Service.Dtos.Movies;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Interfaces;
using MovieLogger.Service.Repositories;

namespace MovieLogger.Service.Services
{
    public class MovieService(
        IMovieRepository movieRepository,
        IGenreRepository genreRepository,
        IMovieWatchRepository movieWatchRepository,
        IMapper mapper) : IMovieService
    {
        public async Task<PagedResult<MovieResponseDto>> SearchAsync(MovieSearchQueryDto query, CancellationToken cancellationToken = default)
        {
            var (movies, totalCount) = await movieRepository.SearchAsync(query, cancellationToken);

            return new PagedResult<MovieResponseDto>
            {
                Items = movies.Select(mapper.Map<MovieResponseDto>).ToList(),
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        public async Task<MovieDetailsResponseDto?> GetDetailsAsync(int id, int? currentUserId, CancellationToken cancellationToken = default)
        {
            var movie = await movieRepository.GetByIdAsync(id, cancellationToken);
            if (movie is null)
            {
                return null;
            }

            var response = new MovieDetailsResponseDto
            {
                Movie = mapper.Map<MovieResponseDto>(movie)
            };

            if (currentUserId.HasValue)
            {
                var history = await movieWatchRepository.GetHistoryForMovieAsync(currentUserId.Value, id, cancellationToken);
                var logs = history.Select(mapper.Map<MovieWatchResponseDto>).ToList();

                response.UserHistory = new UserMovieHistoryDto
                {
                    TimesWatched = logs.Count,
                    LastWatchedAt = logs.Count > 0 ? logs.Max(l => l.DateWatched) : null,
                    LastRating = logs.OrderByDescending(l => l.DateWatched).FirstOrDefault()?.Rating,
                    Logs = logs
                };
            }

            return response;
        }

        public async Task<MovieMutationResult> CreateAsync(CreateMovieDto dto, int createdByUserId, CancellationToken cancellationToken = default)
        {
            var (genres, invalidGenreIds) = await ResolveGenresAsync(dto.GenreIds, cancellationToken);
            if (invalidGenreIds.Count > 0)
            {
                return MovieMutationResult.InvalidGenres(invalidGenreIds);
            }

            var movie = mapper.Map<Movie>(dto);
            movie.Genres = genres;
            movie.CreatedAt = DateTime.UtcNow;
            movie.CreatedByUserId = createdByUserId;

            await movieRepository.AddAsync(movie, cancellationToken);
            return MovieMutationResult.Success(mapper.Map<MovieResponseDto>(movie));
        }

        public async Task<MovieMutationResult> UpdateAsync(int id, UpdateMovieDto dto, int currentUserId, CancellationToken cancellationToken = default)
        {
            var movie = await movieRepository.GetByIdAsync(id, cancellationToken);
            if (movie is null)
            {
                return MovieMutationResult.NotFound();
            }

            // The catalogue is shared, so only the user who added a movie may change it.
            if (movie.CreatedByUserId != currentUserId)
            {
                return MovieMutationResult.NotCreator();
            }

            var (genres, invalidGenreIds) = await ResolveGenresAsync(dto.GenreIds, cancellationToken);
            if (invalidGenreIds.Count > 0)
            {
                return MovieMutationResult.InvalidGenres(invalidGenreIds);
            }

            mapper.Map(dto, movie);

            movie.Genres.Clear();
            foreach (var genre in genres)
            {
                movie.Genres.Add(genre);
            }

            await movieRepository.UpdateAsync(movie, cancellationToken);
            return MovieMutationResult.Success(mapper.Map<MovieResponseDto>(movie));
        }

        public async Task<MovieMutationOutcome> DeleteAsync(int id, int currentUserId, CancellationToken cancellationToken = default)
        {
            var movie = await movieRepository.GetByIdAsync(id, cancellationToken);
            if (movie is null)
            {
                return MovieMutationOutcome.NotFound;
            }

            // Deleting a movie cascades to every user's watch logs, watchlist entries and list items for it,
            // so only the user who added it may delete it.
            if (movie.CreatedByUserId != currentUserId)
            {
                return MovieMutationOutcome.NotCreator;
            }

            var deleted = await movieRepository.DeleteAsync(id, cancellationToken);
            return deleted ? MovieMutationOutcome.Success : MovieMutationOutcome.NotFound;
        }

        private async Task<(List<Genre> Genres, List<int> InvalidGenreIds)> ResolveGenresAsync(
            IReadOnlyList<int> genreIds, CancellationToken cancellationToken)
        {
            if (genreIds.Count == 0)
            {
                return ([], []);
            }

            var genres = await genreRepository.GetByIdsAsync(genreIds, cancellationToken);
            var foundIds = genres.Select(g => g.Id).ToHashSet();
            var invalidGenreIds = genreIds.Where(id => !foundIds.Contains(id)).Distinct().ToList();

            return (genres.ToList(), invalidGenreIds);
        }
    }
}
