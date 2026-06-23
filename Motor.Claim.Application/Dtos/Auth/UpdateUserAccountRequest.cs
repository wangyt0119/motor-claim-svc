using Motor.Claim.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Motor.Claim.Application.Dtos.Auth
{
    public class UpdateUserAccountRequest
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public MobileCountry MobileCountry { get; set; }

        [Required]
        public string MobileNumber { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; }

        public Guid? WorkshopId { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;
    }
}
