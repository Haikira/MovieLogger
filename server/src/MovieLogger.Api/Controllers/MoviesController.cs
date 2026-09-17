using Microsoft.AspNetCore.Mvc;
using MovieLogger.Service.Dtos.Movies;
using MovieLogger.Service.Interfaces;
using MovieLogger.Service.Services;

namespace MovieLogger.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MoviesController(IMovieService movieService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<MovieResponseDto>>> GetAll(CancellationToken cancellationToken)
        {
            return Ok(await movieService.GetAllAsync(cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<MovieResponseDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var movie = await movieService.GetByIdAsync(id, cancellationToken);
            return movie is null ? NotFound() : Ok(movie);
        }

        [HttpPost]
        public async Task<ActionResult<MovieResponseDto>> Create(CreateMovieDto dto, CancellationToken cancellationToken)
        {
            var result = await movieService.CreateAsync(dto, cancellationToken);
            return result.Outcome switch
            {
                MovieMutationOutcome.Success => CreatedAtAction(nameof(GetById), new { id = result.Movie!.Id }, result.Movie),
                MovieMutationOutcome.InvalidGenreIds => BadRequest(new { message = "One or more GenreIds do not exist.", invalidGenreIds = result.InvalidGenreIds }),
                _ => BadRequest()
            };
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateMovieDto dto, CancellationToken cancellationToken)
        {
            var result = await movieService.UpdateAsync(id, dto, cancellationToken);
            return result.Outcome switch
            {
                MovieMutationOutcome.Success => NoContent(),
                MovieMutationOutcome.NotFound => NotFound(),
                MovieMutationOutcome.InvalidGenreIds => BadRequest(new { message = "One or more GenreIds do not exist.", invalidGenreIds = result.InvalidGenreIds }),
                _ => BadRequest()
            };
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var deleted = await movieService.DeleteAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
    }
}
