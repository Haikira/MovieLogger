using AutoMapper;
using MovieLogger.Service.Dtos.Lists;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Interfaces;
using MovieLogger.Service.Repositories;

namespace MovieLogger.Service.Services
{
    public class MovieListService(
        IMovieListRepository movieListRepository,
        IUserRepository userRepository,
        IMapper mapper) : IMovieListService
    {
        public async Task<IReadOnlyList<MovieListResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var lists = await movieListRepository.GetAllAsync(cancellationToken);
            return lists.Select(mapper.Map<MovieListResponseDto>).ToList();
        }

        public async Task<MovieListResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var list = await movieListRepository.GetByIdAsync(id, cancellationToken);
            return list is null ? null : mapper.Map<MovieListResponseDto>(list);
        }

        public async Task<MovieListMutationResult> CreateAsync(CreateMovieListDto dto, CancellationToken cancellationToken = default)
        {
            var user = await userRepository.GetByIdAsync(dto.UserId, cancellationToken);
            if (user is null)
            {
                return MovieListMutationResult.InvalidUser();
            }

            var list = mapper.Map<MovieList>(dto);
            list.CreatedAt = DateTime.UtcNow;

            await movieListRepository.AddAsync(list, cancellationToken);
            return MovieListMutationResult.Success(mapper.Map<MovieListResponseDto>(list));
        }

        public async Task<bool> UpdateAsync(int id, UpdateMovieListDto dto, CancellationToken cancellationToken = default)
        {
            var list = await movieListRepository.GetByIdAsync(id, cancellationToken);
            if (list is null)
            {
                return false;
            }

            mapper.Map(dto, list);
            await movieListRepository.UpdateAsync(list, cancellationToken);
            return true;
        }

        public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            return movieListRepository.DeleteAsync(id, cancellationToken);
        }
    }
}
