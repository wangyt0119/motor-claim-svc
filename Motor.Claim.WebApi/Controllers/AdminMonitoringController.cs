using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Motor.Claim.Application.Services;

namespace Motor.Claim.WebApi.Controllers
{
    [ApiController]
    [Route("api/admin/monitoring")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminMonitoringController : ControllerBase
    {
        private readonly SystemMonitoringService _systemMonitoringService;

        public AdminMonitoringController(SystemMonitoringService systemMonitoringService)
        {
            _systemMonitoringService = systemMonitoringService;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc)
        {
            var result = await _systemMonitoringService.GetDashboardAsync(fromUtc, toUtc);
            return Ok(result);
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs(
            [FromQuery] DateTime? fromUtc,
            [FromQuery] DateTime? toUtc,
            [FromQuery] string? module,
            [FromQuery] Guid? userId,
            [FromQuery] string? userRole,
            [FromQuery] int take = 200)
        {
            var result = await _systemMonitoringService.GetLogsAsync(fromUtc, toUtc, module, userId, userRole, take);
            return Ok(result);
        }

        [HttpGet("logs/export")]
        public async Task<IActionResult> ExportLogs(
            [FromQuery] DateTime? fromUtc,
            [FromQuery] DateTime? toUtc,
            [FromQuery] string? module,
            [FromQuery] Guid? userId,
            [FromQuery] string? userRole,
            [FromQuery] int take = 1000)
        {
            var payload = await _systemMonitoringService.ExportLogsCsvAsync(fromUtc, toUtc, module, userId, userRole, take);
            var fileName = $"system-activity-logs-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            return File(payload, "text/csv", fileName);
        }
    }
}
