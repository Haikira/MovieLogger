using AutoMapper;
using MovieLogger.Service.Dtos.Users;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Interfaces;
using MovieLogger.Service.Repositories;

namespace MovieLogger.Service.Services
{
    public class UserMovieService(
        IUserMovieRepository userMovieRepository,
        IUserRepository userRepository,
        IMovieRepository movieRepository,
        IMapper mapper) : IUserMovieService
    {
        public async Task<IReadOnlyList<UserMovieResponseDto>?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = await userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
            {
                return null;
            }

            var userMovies = await userMovieRepository.GetByUserIdAsync(userId, cancellationToken);
            return userMovies.Select(mapper.Map<UserMovieResponseDto>).ToList();
        }

        public async Task<SetUserMovieStatusResult> SetStatusAsync(int userId, int movieId, SetUserMovieStatusDto dto, CancellationToken cancellationToken = default)
        {
            var user = await userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
            {
                return SetUserMovieStatusResult.UserNotFound();
            }

            var movie = await movieRepository.GetByIdAsync(movieId, cancellationToken);
            if (movie is null)
            {
                return SetUserMovieStatusResult.MovieNotFound();
            }

            var userMovie = new UserMovie
            {
                UserId = userId,
                MovieId = movieId,
                IsFavourite = dto.IsFavourite,
                IsOwned = dto.IsOwned
            };

            await userMovieRepository.UpsertAsync(userMovie, cancellationToken);
            return SetUserMovieStatusResult.Success();
        }

        public Task<bool> RemoveAsync(int userId, int movieId, CancellationToken cancellationToken = default)
        {
            return userMovieRepository.DeleteAsync(userId, movieId, cancellationToken);
        }
    }
}
