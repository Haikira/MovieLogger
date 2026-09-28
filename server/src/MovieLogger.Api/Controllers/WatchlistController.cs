using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieLogger.Api.Security;
using MovieLogger.Service.Dtos.Watchlist;
using MovieLogger.Service.Interfaces;
using MovieLogger.Service.Services;

namespace MovieLogger.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class WatchlistController(IWatchlistService watchlistService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<WatchlistItemResponseDto>>> GetMine(CancellationToken cancellationToken)
        {
            return Ok(await watchlistService.GetMineAsync(User.GetUserId(), cancellationToken));
        }

        [HttpPost("{movieId:int}")]
        public async Task<ActionResult<WatchlistItemResponseDto>> Add(int movieId, CancellationToken cancellationToken)
        {
            var result = await watchlistService.AddAsync(User.GetUserId(), movieId, cancellationToken);
            return result.Outcome switch
            {
                AddToWatchlistOutcome.Success => CreatedAtAction(nameof(GetMine), null, result.WatchlistItem),
                AddToWatchlistOutcome.MovieNotFound => BadRequest(new { message = "MovieId does not exist." }),
                AddToWatchlistOutcome.AlreadyExists => Conflict(new { message = "This movie is already on your watchlist." }),
                _ => BadRequest()
            };
        }

        [HttpDelete("{movieId:int}")]
        public async Task<IActionResult> Remove(int movieId, CancellationToken cancellationToken)
        {
            var removed = await watchlistService.RemoveAsync(User.GetUserId(), movieId, cancellationToken);
            return removed ? NoContent() : NotFound();
        }
    }
}
