using Motor.Claim.Domain.Entities;
using Motor.Claim.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Motor.Claim.Application.Dtos.Auth
{
    public class RegisterUserRequest
    {
        [Required]
        public string FullName { get; set; }

        [Required]
        public IdType IdType { get; set; }

        public string? NRIC { get; set; }

        public string? PassportNo { get; set; }

        public string? IssueCountry { get; set; }

        [Required]
        public MobileCountry MobileCountry { get; set; }

        [Required]
        public string MobileNumber { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public bool IsMaybankGroupEmployee { get; set; }

        [Required]
        public string Password { get; set; }
    }
}