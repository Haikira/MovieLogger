using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieLogger.Api.Security;
using MovieLogger.Service.Dtos.Dashboard;
using MovieLogger.Service.Interfaces;

namespace MovieLogger.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class DashboardController(IDashboardService dashboardService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<DashboardResponseDto>> Get(CancellationToken cancellationToken)
        {
            return Ok(await dashboardService.GetDashboardAsync(User.GetUserId(), cancellationToken));
        }
    }
}
