namespace Motor.Claim.Application.Features.WorkshopAppointment.Queries
{
    public class GetWorkshopAppointmentByClaimQuery
    {
        public Guid UserId { get; set; }
        public Guid ClaimId { get; set; }
        public bool EnforceOwnership { get; set; } = true;
    }
}
