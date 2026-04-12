using System.Text;
using Motor.Claim.Application.Dtos.Admin;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.Domain.Entities;

namespace Motor.Claim.Application.Services
{
    public class SystemMonitoringService
    {
        private readonly ISystemActivityLogRepository _systemActivityLogRepository;

        public SystemMonitoringService(ISystemActivityLogRepository systemActivityLogRepository)
        {
            _systemActivityLogRepository = systemActivityLogRepository;
        }

        public async Task LogAsync(SystemActivityLogEntity log)
        {
            await _systemActivityLogRepository.AddAsync(log);
        }

        public async Task<SystemMonitoringDashboardResponse> GetDashboardAsync(DateTime? fromUtc, DateTime? toUtc)
        {
            var logs = await _systemActivityLogRepository.GetFilteredAsync(fromUtc, toUtc, null, null, null, 5000);

            return new SystemMonitoringDashboardResponse
            {
                GeneratedAtUtc = DateTime.UtcNow,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                TotalRequests = logs.Count,
                SuccessfulRequests = logs.Count(x => x.IsSuccess),
                FailedRequests = logs.Count(x => !x.IsSuccess),
                AverageDurationMs = logs.Count == 0 ? 0 : logs.Average(x => x.DurationMs),
                ModuleUsage = logs
                    .GroupBy(x => x.Module)
                    .OrderByDescending(x => x.Count())
                    .Select(x => new SystemUsageStatResponse
                    {
                        Module = x.Key,
                        RequestCount = x.Count()
                    })
                    .ToList(),
                RecentLogs = logs
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(20)
                    .Select(MapLog)
                    .ToList()
            };
        }

        public async Task<List<SystemActivityLogResponse>> GetLogsAsync(
            DateTime? fromUtc,
            DateTime? toUtc,
            string? module,
            Guid? userId,
            string? userRole,
            int take)
        {
            var logs = await _systemActivityLogRepository.GetFilteredAsync(fromUtc, toUtc, module, userId, userRole, take);
            return logs.Select(MapLog).ToList();
        }

        public async Task<byte[]> ExportLogsCsvAsync(
            DateTime? fromUtc,
            DateTime? toUtc,
            string? module,
            Guid? userId,
            string? userRole,
            int take)
        {
            var logs = await GetLogsAsync(fromUtc, toUtc, module, userId, userRole, take);
            var builder = new StringBuilder();
            builder.AppendLine("CreatedAtUtc,Module,Action,HttpMethod,Path,StatusCode,DurationMs,IsSuccess,UserId,UserRole,UserEmail,IpAddress,ErrorMessage");

            foreach (var log in logs)
            {
                builder.AppendLine(string.Join(",",
                    Escape(log.CreatedAt.ToString("O")),
                    Escape(log.Module),
                    Escape(log.Action),
                    Escape(log.HttpMethod),
                    Escape(log.Path + (string.IsNullOrWhiteSpace(log.QueryString) ? string.Empty : log.QueryString)),
                    log.StatusCode,
                    log.DurationMs,
                    log.IsSuccess,
                    Escape(log.UserId?.ToString()),
                    Escape(log.UserRole),
                    Escape(log.UserEmail),
                    Escape(log.IpAddress),
                    Escape(log.ErrorMessage)));
            }

            return Encoding.UTF8.GetBytes(builder.ToString());
        }

        public static SystemActivityLogResponse MapLog(SystemActivityLogEntity log)
        {
            return new SystemActivityLogResponse
            {
                LogId = log.LogId,
                CreatedAt = log.CreatedAt,
                UserId = log.UserId,
                UserRole = log.UserRole,
                UserEmail = log.UserEmail,
                Module = log.Module,
                Action = log.Action,
                HttpMethod = log.HttpMethod,
                Path = log.Path,
                QueryString = log.QueryString,
                StatusCode = log.StatusCode,
                DurationMs = log.DurationMs,
                IsSuccess = log.IsSuccess,
                IpAddress = log.IpAddress,
                ErrorMessage = log.ErrorMessage
            };
        }

        private static string Escape(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}
