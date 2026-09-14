using AutoMapper;
using MovieLogger.Service.Dtos.MovieWatches;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Interfaces;
using MovieLogger.Service.Repositories;

namespace MovieLogger.Service.Services
{
    public class MovieWatchService(
        IMovieWatchRepository movieWatchRepository,
        IUserRepository userRepository,
        IMovieRepository movieRepository,
        IMapper mapper) : IMovieWatchService
    {
        public async Task<IReadOnlyList<MovieWatchResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var watches = await movieWatchRepository.GetAllAsync(cancellationToken);
            return watches.Select(mapper.Map<MovieWatchResponseDto>).ToList();
        }

        public async Task<MovieWatchResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var watch = await movieWatchRepository.GetByIdAsync(id, cancellationToken);
            return watch is null ? null : mapper.Map<MovieWatchResponseDto>(watch);
        }

        public async Task<MovieWatchMutationResult> CreateAsync(CreateMovieWatchDto dto, CancellationToken cancellationToken = default)
        {
            var user = await userRepository.GetByIdAsync(dto.UserId, cancellationToken);
            if (user is null)
            {
                return MovieWatchMutationResult.InvalidUser();
            }

            var movie = await movieRepository.GetByIdAsync(dto.MovieId, cancellationToken);
            if (movie is null)
            {
                return MovieWatchMutationResult.InvalidMovie();
            }

            var watch = mapper.Map<MovieWatch>(dto);
            await movieWatchRepository.AddAsync(watch, cancellationToken);
            return MovieWatchMutationResult.Success(mapper.Map<MovieWatchResponseDto>(watch));
        }

        public async Task<bool> UpdateAsync(int id, UpdateMovieWatchDto dto, CancellationToken cancellationToken = default)
        {
            var watch = await movieWatchRepository.GetByIdAsync(id, cancellationToken);
            if (watch is null)
            {
                return false;
            }

            mapper.Map(dto, watch);
            await movieWatchRepository.UpdateAsync(watch, cancellationToken);
            return true;
        }

        public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            return movieWatchRepository.DeleteAsync(id, cancellationToken);
        }
    }
}
