using Microsoft.AspNetCore.Mvc;
using MovieLogger.Service.Dtos.Auth;
using MovieLogger.Service.Interfaces;
using MovieLogger.Service.Services;

namespace MovieLogger.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterRequestDto dto, CancellationToken cancellationToken)
        {
            var result = await authService.RegisterAsync(dto, cancellationToken);
            return result.Outcome switch
            {
                RegisterOutcome.Success => Ok(result.AuthResponse),
                RegisterOutcome.DuplicateEmail => Conflict(new { message = "A user with this email already exists." }),
                _ => BadRequest()
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginRequestDto dto, CancellationToken cancellationToken)
        {
            var result = await authService.LoginAsync(dto, cancellationToken);
            return result.Outcome switch
            {
                LoginOutcome.Success => Ok(result.AuthResponse),
                LoginOutcome.InvalidCredentials => Unauthorized(new { message = "Invalid email or password." }),
                _ => BadRequest()
            };
        }
    }
}
