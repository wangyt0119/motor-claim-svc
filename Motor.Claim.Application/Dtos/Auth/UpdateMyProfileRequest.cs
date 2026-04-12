using System.ComponentModel.DataAnnotations;
using Motor.Claim.Domain.Enums;

namespace Motor.Claim.Application.Dtos.Auth
{
    public class UpdateMyProfileRequest
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public IdType IdType { get; set; }

        public string? Nric { get; set; }
        public string? PassportNo { get; set; }
        public string? IssueCountry { get; set; }

        [Required]
        public MobileCountry MobileCountry { get; set; }

        [Required]
        public string MobileNumber { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
