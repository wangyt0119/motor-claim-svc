namespace Motor.Claim.Application.Dtos.Admin
{
    public class SystemActivityLogResponse
    {
        public Guid LogId { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? UserId { get; set; }
        public string? UserRole { get; set; }
        public string? UserEmail { get; set; }
        public string Module { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string HttpMethod { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string? QueryString { get; set; }
        public int StatusCode { get; set; }
        public long DurationMs { get; set; }
        public bool IsSuccess { get; set; }
        public string? IpAddress { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
