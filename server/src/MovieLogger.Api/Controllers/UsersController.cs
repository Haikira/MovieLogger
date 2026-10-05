using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieLogger.Api.Security;
using MovieLogger.Service.Dtos.Users;
using MovieLogger.Service.Interfaces;
using MovieLogger.Service.Services;

namespace MovieLogger.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class UsersController(IUserService userService, IUserMovieService userMovieService) : ControllerBase
    {
        [HttpGet("me")]
        public async Task<ActionResult<UserResponseDto>> GetMe(CancellationToken cancellationToken)
        {
            var user = await userService.GetByIdAsync(User.GetUserId(), cancellationToken);
            return user is null ? NotFound() : Ok(user);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserResponseDto>> GetById(int id, CancellationToken cancellationToken)
        {
            if (id != User.GetUserId())
            {
                return Forbid();
            }

            var user = await userService.GetByIdAsync(id, cancellationToken);
            return user is null ? NotFound() : Ok(user);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateUserDto dto, CancellationToken cancellationToken)
        {
            if (id != User.GetUserId())
            {
                return Forbid();
            }

            var result = await userService.UpdateAsync(id, dto, cancellationToken);
            return result.Outcome switch
            {
                UpdateUserOutcome.Success => NoContent(),
                UpdateUserOutcome.UserNotFound => NotFound(),
                UpdateUserOutcome.DuplicateEmail => Conflict(new { message = "A user with this email already exists." }),
                _ => BadRequest()
            };
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            if (id != User.GetUserId())
            {
                return Forbid();
            }

            var deleted = await userService.DeleteAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto, CancellationToken cancellationToken)
        {
            var result = await userService.ChangePasswordAsync(User.GetUserId(), dto, cancellationToken);
            return result.Outcome switch
            {
                ChangePasswordOutcome.Success => NoContent(),
                ChangePasswordOutcome.IncorrectCurrentPassword => BadRequest(new { message = "Current password is incorrect." }),
                ChangePasswordOutcome.UserNotFound => NotFound(),
                _ => BadRequest()
            };
        }

        [HttpGet("{userId:int}/Movies")]
        public async Task<ActionResult<IReadOnlyList<UserMovieResponseDto>>> GetMovies(int userId, CancellationToken cancellationToken)
        {
            if (userId != User.GetUserId())
            {
                return Forbid();
            }

            var userMovies = await userMovieService.GetByUserIdAsync(userId, cancellationToken);
            return userMovies is null ? NotFound() : Ok(userMovies);
        }

        [HttpPut("{userId:int}/Movies/{movieId:int}")]
        public async Task<IActionResult> SetMovieStatus(int userId, int movieId, SetUserMovieStatusDto dto, CancellationToken cancellationToken)
        {
            if (userId != User.GetUserId())
            {
                return Forbid();
            }

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
            if (userId != User.GetUserId())
            {
                return Forbid();
            }

            var removed = await userMovieService.RemoveAsync(userId, movieId, cancellationToken);
            return removed ? NoContent() : NotFound();
        }
    }
}
