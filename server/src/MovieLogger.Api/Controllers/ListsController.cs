using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieLogger.Api.Security;
using MovieLogger.Service.Dtos.Lists;
using MovieLogger.Service.Interfaces;
using MovieLogger.Service.Services;

namespace MovieLogger.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ListsController(IMovieListService movieListService, IListMovieService listMovieService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<MovieListResponseDto>>> GetAll(CancellationToken cancellationToken)
        {
            return Ok(await movieListService.GetMineAsync(User.GetUserId(), cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<MovieListResponseDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var list = await movieListService.GetByIdAsync(id, User.GetUserId(), cancellationToken);
            return list is null ? NotFound() : Ok(list);
        }

        [HttpPost]
        public async Task<ActionResult<MovieListResponseDto>> Create(CreateMovieListDto dto, CancellationToken cancellationToken)
        {
            var result = await movieListService.CreateAsync(dto, User.GetUserId(), cancellationToken);
            return result.Outcome switch
            {
                MovieListMutationOutcome.Success => CreatedAtAction(nameof(GetById), new { id = result.MovieList!.Id }, result.MovieList),
                MovieListMutationOutcome.InvalidUserId => NotFound(),
                _ => BadRequest()
            };
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateMovieListDto dto, CancellationToken cancellationToken)
        {
            var updated = await movieListService.UpdateAsync(id, dto, User.GetUserId(), cancellationToken);
            return updated ? NoContent() : NotFound();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var deleted = await movieListService.DeleteAsync(id, User.GetUserId(), cancellationToken);
            return deleted ? NoContent() : NotFound();
        }

        [HttpPost("{listId:int}/Movies/{movieId:int}")]
        public async Task<IActionResult> AddMovie(int listId, int movieId, CancellationToken cancellationToken)
        {
            var result = await listMovieService.AddAsync(listId, movieId, User.GetUserId(), cancellationToken);
            return result.Outcome switch
            {
                AddMovieToListOutcome.Success => NoContent(),
                AddMovieToListOutcome.ListNotFound => NotFound(),
                AddMovieToListOutcome.MovieNotFound => BadRequest(new { message = "MovieId does not exist." }),
                _ => BadRequest()
            };
        }

        [HttpDelete("{listId:int}/Movies/{movieId:int}")]
        public async Task<IActionResult> RemoveMovie(int listId, int movieId, CancellationToken cancellationToken)
        {
            var removed = await listMovieService.RemoveAsync(listId, movieId, User.GetUserId(), cancellationToken);
            return removed ? NoContent() : NotFound();
        }
    }
}
