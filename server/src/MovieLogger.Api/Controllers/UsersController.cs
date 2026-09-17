using Microsoft.AspNetCore.Mvc;
using MovieLogger.Service.Dtos.Users;
using MovieLogger.Service.Interfaces;
using MovieLogger.Service.Services;

namespace MovieLogger.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController(IUserService userService, IUserMovieService userMovieService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<UserResponseDto>>> GetAll(CancellationToken cancellationToken)
        {
            return Ok(await userService.GetAllAsync(cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserResponseDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var user = await userService.GetByIdAsync(id, cancellationToken);
            return user is null ? NotFound() : Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<UserResponseDto>> Create(CreateUserDto dto, CancellationToken cancellationToken)
        {
            var user = await userService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateUserDto dto, CancellationToken cancellationToken)
        {
            var updated = await userService.UpdateAsync(id, dto, cancellationToken);
            return updated ? NoContent() : NotFound();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var deleted = await userService.DeleteAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }

        [HttpGet("{userId:int}/Movies")]
        public async Task<ActionResult<IReadOnlyList<UserMovieResponseDto>>> GetMovies(int userId, CancellationToken cancellationToken)
        {
            var userMovies = await userMovieService.GetByUserIdAsync(userId, cancellationToken);
            return userMovies is null ? NotFound() : Ok(userMovies);
        }

        [HttpPut("{userId:int}/Movies/{movieId:int}")]
        public async Task<IActionResult> SetMovieStatus(int userId, int movieId, SetUserMovieStatusDto dto, CancellationToken cancellationToken)
        {
            var result = await userMovieService.SetStatusAsync(userId, movieId, dto, cancellationToken);
            return result.Outcome switch
            {
                SetUserMovieStatusOutcome.Success => NoContent(),
                SetUserMovieStatusOutcome.UserNotFound => NotFound(),
                SetUserMovieStatusOutcome.MovieNotFound => BadRequest(new { message = "MovieId does not exist." }),
                _ => BadRequest()
            };
        }

        [HttpDelete("{userId:int}/Movies/{movieId:int}")]
        public async Task<IActionResult> RemoveMovieStatus(int userId, int movieId, CancellationToken cancellationToken)
        {
            var removed = await userMovieService.RemoveAsync(userId, movieId, cancellationToken);
            return removed ? NoContent() : NotFound();
        }
    }
}
