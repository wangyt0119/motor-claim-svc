using System.ComponentModel.DataAnnotations;

namespace Motor.Claim.Application.Dtos.Auth
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
