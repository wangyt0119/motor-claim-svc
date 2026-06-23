using System.ComponentModel.DataAnnotations;

namespace Motor.Claim.Application.Dtos.Claim
{
    public class WithdrawClaimRequest
    {
        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
    }
}
