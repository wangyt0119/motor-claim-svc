using Motor.Claim.Application.Dtos.Workshop;
using Motor.Claim.Domain.Enums;

namespace Motor.Claim.Application.Dtos.Auth
{
    public class UserProfileResponse
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public IdType IdType { get; set; }
        public string? Nric { get; set; }
        public string? PassportNo { get; set; }
        public string? IssueCountry { get; set; }
        public MobileCountry MobileCountry { get; set; }
        public string MobileNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsMaybankGroupEmployee { get; set; }
        public UserRole Role { get; set; }
        public bool IsActive { get; set; }
        public Guid? WorkshopId { get; set; }
        public WorkshopResponse? Workshop { get; set; }
    }
}
