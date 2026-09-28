using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieLogger.Api.Security;
using MovieLogger.Service.Dtos.Common;
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
        public async Task<ActionResult<PagedResult<MovieResponseDto>>> Search([FromQuery] MovieSearchQueryDto query, CancellationToken cancellationToken)
        {
            return Ok(await movieService.SearchAsync(query, cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<MovieDetailsResponseDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var details = await movieService.GetDetailsAsync(id, User.GetUserIdOrDefault(), cancellationToken);
            return details is null ? NotFound() : Ok(details);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<MovieResponseDto>> Create(CreateMovieDto dto, CancellationToken cancellationToken)
        {
            var result = await movieService.CreateAsync(dto, User.GetUserId(), cancellationToken);
            return result.Outcome switch
            {
                MovieMutationOutcome.Success => CreatedAtAction(nameof(GetById), new { id = result.Movie!.Id }, result.Movie),
                MovieMutationOutcome.InvalidGenreIds => BadRequest(new { message = "One or more GenreIds do not exist.", invalidGenreIds = result.InvalidGenreIds }),
                _ => BadRequest()
            };
        }

        [Authorize]
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

        [Authorize]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var deleted = await movieService.DeleteAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
    }
}
