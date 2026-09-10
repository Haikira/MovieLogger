using AutoMapper;
using MovieLogger.Service.Dtos.Genres;
using MovieLogger.Service.Entities;
using MovieLogger.Service.Interfaces;
using MovieLogger.Service.Repositories;

namespace MovieLogger.Service.Services
{
    public class GenreService(IGenreRepository genreRepository, IMapper mapper) : IGenreService
    {
        public async Task<IReadOnlyList<GenreResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var genres = await genreRepository.GetAllAsync(cancellationToken);
            return genres.Select(mapper.Map<GenreResponseDto>).ToList();
        }

        public async Task<GenreResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var genre = await genreRepository.GetByIdAsync(id, cancellationToken);
            return genre is null ? null : mapper.Map<GenreResponseDto>(genre);
        }

        public async Task<GenreResponseDto> CreateAsync(CreateGenreDto dto, CancellationToken cancellationToken = default)
        {
            var genre = mapper.Map<Genre>(dto);
            await genreRepository.AddAsync(genre, cancellationToken);
            return mapper.Map<GenreResponseDto>(genre);
        }

        public async Task<bool> UpdateAsync(int id, UpdateGenreDto dto, CancellationToken cancellationToken = default)
        {
            var genre = await genreRepository.GetByIdAsync(id, cancellationToken);
            if (genre is null)
            {
                return false;
            }

            mapper.Map(dto, genre);
            await genreRepository.UpdateAsync(genre, cancellationToken);
            return true;
        }

        public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            return genreRepository.DeleteAsync(id, cancellationToken);
        }
    }
}
