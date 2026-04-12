namespace Motor.Claim.Application.Dtos.Admin
{
    public class SystemUsageStatResponse
    {
        public string Module { get; set; } = string.Empty;
        public int RequestCount { get; set; }
    }
}
