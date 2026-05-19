using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Motor.Claim.Domain.Entities
{
    public class WorkshopAppointmentEntity
    {
        public DateTime CreatedAt { get; set; }

        [Key]
        public Guid AppointmentId { get; set; }

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

        [Required]
        public string Status { get; set; } = "Pending";

        public string? Notes { get; set; }

        [NotMapped]
        public bool? EmailNotificationSent { get; set; }

        [NotMapped]
        public string? EmailNotificationMessage { get; set; }

        [ForeignKey("ClaimId")]
        public ClaimEntity Claim { get; set; } = null!;

        [ForeignKey("WorkshopId")]
        public WorkshopEntity Workshop { get; set; } = null!;
    }
}
