using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieLogger.Api.Security;
using MovieLogger.Service.Dtos.Common;
using MovieLogger.Service.Dtos.MovieWatches;
using MovieLogger.Service.Interfaces;
using MovieLogger.Service.Services;

namespace MovieLogger.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class MovieWatchesController(IMovieWatchService movieWatchService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<MovieWatchResponseDto>>> GetAll(CancellationToken cancellationToken)
        {
            return Ok(await movieWatchService.GetMineAsync(User.GetUserId(), cancellationToken));
        }

        [HttpGet("my-movies")]
        public async Task<ActionResult<PagedResult<MyMovieResponseDto>>> GetMyMovies([FromQuery] MyMoviesQueryDto query, CancellationToken cancellationToken)
        {
            return Ok(await movieWatchService.GetMyMoviesAsync(User.GetUserId(), query, cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<MovieWatchResponseDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var watch = await movieWatchService.GetByIdAsync(id, User.GetUserId(), cancellationToken);
            return watch is null ? NotFound() : Ok(watch);
        }

        [HttpPost]
        public async Task<ActionResult<MovieWatchResponseDto>> Create(CreateMovieWatchDto dto, CancellationToken cancellationToken)
        {
            var result = await movieWatchService.CreateAsync(dto, User.GetUserId(), cancellationToken);
            return result.Outcome switch
            {
                MovieWatchMutationOutcome.Success => CreatedAtAction(nameof(GetById), new { id = result.MovieWatch!.Id }, result.MovieWatch),
                MovieWatchMutationOutcome.InvalidMovieId => BadRequest(new { message = "MovieId does not exist." }),
                _ => BadRequest()
            };
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateMovieWatchDto dto, CancellationToken cancellationToken)
        {
            var updated = await movieWatchService.UpdateAsync(id, dto, User.GetUserId(), cancellationToken);
            return updated ? NoContent() : NotFound();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var deleted = await movieWatchService.DeleteAsync(id, User.GetUserId(), cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
    }
}
