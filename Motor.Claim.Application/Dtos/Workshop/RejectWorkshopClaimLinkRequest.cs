using System.ComponentModel.DataAnnotations;

namespace Motor.Claim.Application.Dtos.Workshop
{
    public class RejectWorkshopClaimLinkRequest
    {
        [MaxLength(500)]
        public string? Reason { get; set; }
    }
}
