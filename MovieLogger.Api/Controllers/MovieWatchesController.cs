using Microsoft.AspNetCore.Mvc;
using MovieLogger.Service.Dtos.MovieWatches;
using MovieLogger.Service.Interfaces;
using MovieLogger.Service.Services;

namespace MovieLogger.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MovieWatchesController(IMovieWatchService movieWatchService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<MovieWatchResponseDto>>> GetAll(CancellationToken cancellationToken)
        {
            return Ok(await movieWatchService.GetAllAsync(cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<MovieWatchResponseDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var watch = await movieWatchService.GetByIdAsync(id, cancellationToken);
            return watch is null ? NotFound() : Ok(watch);
        }

        [HttpPost]
        public async Task<ActionResult<MovieWatchResponseDto>> Create(CreateMovieWatchDto dto, CancellationToken cancellationToken)
        {
            var result = await movieWatchService.CreateAsync(dto, cancellationToken);
            return result.Outcome switch
            {
                MovieWatchMutationOutcome.Success => CreatedAtAction(nameof(GetById), new { id = result.MovieWatch!.Id }, result.MovieWatch),
                MovieWatchMutationOutcome.InvalidUserId => BadRequest(new { message = "UserId does not exist." }),
                MovieWatchMutationOutcome.InvalidMovieId => BadRequest(new { message = "MovieId does not exist." }),
                _ => BadRequest()
            };
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateMovieWatchDto dto, CancellationToken cancellationToken)
        {
            var updated = await movieWatchService.UpdateAsync(id, dto, cancellationToken);
            return updated ? NoContent() : NotFound();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var deleted = await movieWatchService.DeleteAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
    }
}
