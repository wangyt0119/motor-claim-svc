namespace Motor.Claim.Application.Dtos.Admin
{
    public class SystemMonitoringDashboardResponse
    {
        public DateTime GeneratedAtUtc { get; set; }
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public double AverageDurationMs { get; set; }
        public List<SystemUsageStatResponse> ModuleUsage { get; set; } = new();
        public List<SystemActivityLogResponse> RecentLogs { get; set; } = new();
    }
}
