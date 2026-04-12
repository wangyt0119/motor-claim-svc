namespace Motor.Claim.Application.Features.WorkshopAppointment.Commands
{
    public class CreateOrUpdateWorkshopAppointmentCommand
    {
        public Guid UserId { get; set; }
        public Guid ClaimId { get; set; }
        public Guid WorkshopId { get; set; }
        public DateTime PreferredDate { get; set; }
        public TimeSpan TimeSlotStart { get; set; }
        public TimeSpan TimeSlotEnd { get; set; }
        public string? Notes { get; set; }
    }
}
