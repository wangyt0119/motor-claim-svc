using System.ComponentModel.DataAnnotations;

namespace Motor.Claim.Application.Dtos.Workshop
{
    public class CreateWorkshopAppointmentRequest
    {
        [Required]
        public Guid ClaimId { get; set; }

        [Required]
        public Guid WorkshopId { get; set; }

        [Required]
        public DateTime PreferredDate { get; set; }

        [Required]
        public TimeSpan TimeSlotStart { get; set; }

        [Required]
        public TimeSpan TimeSlotEnd { get; set; }

        public string? Notes { get; set; }
    }
}
