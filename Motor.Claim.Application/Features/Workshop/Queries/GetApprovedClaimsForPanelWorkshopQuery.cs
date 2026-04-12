using Motor.Claim.Application.Dtos.Claim;

namespace Motor.Claim.Application.Features.Workshop.Queries
{
    public class GetApprovedClaimsForPanelWorkshopQuery
    {
        public Guid WorkshopId { get; set; }
    }
}
