using Microsoft.AspNetCore.Mvc;
using MovieLogger.Service.Dtos.Genres;
using MovieLogger.Service.Interfaces;

namespace MovieLogger.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GenresController(IGenreService genreService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<GenreResponseDto>>> GetAll(CancellationToken cancellationToken)
        {
            return Ok(await genreService.GetAllAsync(cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<GenreResponseDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var genre = await genreService.GetByIdAsync(id, cancellationToken);
            return genre is null ? NotFound() : Ok(genre);
        }

        [HttpPost]
        public async Task<ActionResult<GenreResponseDto>> Create(CreateGenreDto dto, CancellationToken cancellationToken)
        {
            var genre = await genreService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = genre.Id }, genre);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateGenreDto dto, CancellationToken cancellationToken)
        {
            var updated = await genreService.UpdateAsync(id, dto, cancellationToken);
            return updated ? NoContent() : NotFound();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var deleted = await genreService.DeleteAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
    }
}
