using AutoMapper;
using MovieLogger.Service.Dtos.Common;
using MovieLogger.Service.Dtos.MovieWatches;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Interfaces;
using MovieLogger.Service.Repositories;

namespace MovieLogger.Service.Services
{
    public class MovieWatchService(
        IMovieWatchRepository movieWatchRepository,
        IMovieRepository movieRepository,
        IMapper mapper) : IMovieWatchService
    {
        public async Task<IReadOnlyList<MovieWatchResponseDto>> GetMineAsync(int userId, CancellationToken cancellationToken = default)
        {
            var watches = await movieWatchRepository.GetByUserIdAsync(userId, cancellationToken);
            return watches.Select(mapper.Map<MovieWatchResponseDto>).ToList();
        }

        public async Task<MovieWatchResponseDto?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default)
        {
            var watch = await movieWatchRepository.GetByIdAsync(id, cancellationToken);
            return watch is null || watch.UserId != userId ? null : mapper.Map<MovieWatchResponseDto>(watch);
        }

        public async Task<MovieWatchMutationResult> CreateAsync(CreateMovieWatchDto dto, int userId, CancellationToken cancellationToken = default)
        {
            var movie = await movieRepository.GetByIdAsync(dto.MovieId, cancellationToken);
            if (movie is null)
            {
                return MovieWatchMutationResult.InvalidMovie();
            }

            var watch = mapper.Map<MovieWatch>(dto);
            watch.UserId = userId;
            watch.CreatedAt = DateTime.UtcNow;

            await movieWatchRepository.AddAsync(watch, cancellationToken);
            return MovieWatchMutationResult.Success(mapper.Map<MovieWatchResponseDto>(watch));
        }

        public async Task<bool> UpdateAsync(int id, UpdateMovieWatchDto dto, int userId, CancellationToken cancellationToken = default)
        {
            var watch = await movieWatchRepository.GetByIdAsync(id, cancellationToken);
            if (watch is null || watch.UserId != userId)
            {
                return false;
            }

            mapper.Map(dto, watch);
            watch.UpdatedAt = DateTime.UtcNow;
            await movieWatchRepository.UpdateAsync(watch, cancellationToken);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, int userId, CancellationToken cancellationToken = default)
        {
            var watch = await movieWatchRepository.GetByIdAsync(id, cancellationToken);
            if (watch is null || watch.UserId != userId)
            {
                return false;
            }

            return await movieWatchRepository.DeleteAsync(id, cancellationToken);
        }

        public async Task<PagedResult<MyMovieResponseDto>> GetMyMoviesAsync(int userId, MyMoviesQueryDto query, CancellationToken cancellationToken = default)
        {
            var (items, totalCount) = await movieWatchRepository.SearchMyMoviesAsync(userId, query, cancellationToken);

            var mapped = items.Select(a => new MyMovieResponseDto
            {
                MovieId = a.Movie.Id,
                Title = a.Movie.Title,
                ReleaseYear = a.Movie.ReleaseYear,
                Director = a.Movie.Director,
                RuntimeMinutes = a.Movie.RuntimeMinutes,
                PosterImageUrl = a.Movie.PosterImageUrl,
                LastWatchedAt = a.LastWatchedAt,
                LastRating = a.LastRating,
                TimesWatched = a.TimesWatched
            }).ToList();

            return new PagedResult<MyMovieResponseDto>
            {
                Items = mapped,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        public async Task<IReadOnlyList<MovieWatchResponseDto>> GetHistoryForMovieAsync(int movieId, int userId, CancellationToken cancellationToken = default)
        {
            var history = await movieWatchRepository.GetHistoryForMovieAsync(userId, movieId, cancellationToken);
            return history.Select(mapper.Map<MovieWatchResponseDto>).ToList();
        }
    }
}
