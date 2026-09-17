using MovieLogger.Service.Dtos.Genres;

namespace MovieLogger.Service.Interfaces
{
    public interface IGenreService
    {
        Task<IReadOnlyList<GenreResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<GenreResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<GenreResponseDto> CreateAsync(CreateGenreDto dto, CancellationToken cancellationToken = default);

        Task<bool> UpdateAsync(int id, UpdateGenreDto dto, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
