namespace Motor.Claim.Application.Dtos.Workshop
{
    public class WorkshopAppointmentResponse
    {
        public Guid AppointmentId { get; set; }
        public Guid ClaimId { get; set; }
        public Guid WorkshopId { get; set; }
        public string WorkshopName { get; set; } = string.Empty;
        public string WorkshopState { get; set; } = string.Empty;
        public string WorkshopAddress { get; set; } = string.Empty;
        public DateTime PreferredDate { get; set; }
        public TimeSpan TimeSlotStart { get; set; }
        public TimeSpan TimeSlotEnd { get; set; }
        public string Status { get; set; } = string.Empty;
        public string AssignmentType { get; set; } = string.Empty;
        public string? WorkshopReferenceNumber { get; set; }
        public string? Notes { get; set; }
        public bool? EmailNotificationSent { get; set; }
        public string? EmailNotificationMessage { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
