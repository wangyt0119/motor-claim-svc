namespace Motor.Claim.Application.Dtos.Auth
{
    public class LoginResponse
    {
        public string Token { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public Motor.Claim.Domain.Enums.UserRole Role { get; set; }
        public Guid? WorkshopId { get; set; }
        public string? WorkshopName { get; set; }
    }
}
