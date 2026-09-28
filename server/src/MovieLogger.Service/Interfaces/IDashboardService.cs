using MovieLogger.Service.Dtos.Dashboard;

namespace MovieLogger.Service.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardResponseDto> GetDashboardAsync(int userId, CancellationToken cancellationToken = default);
    }
}
