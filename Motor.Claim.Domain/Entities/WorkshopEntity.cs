using System.ComponentModel.DataAnnotations;

namespace Motor.Claim.Domain.Entities
{
    public class WorkshopEntity
    {
        public DateTime CreatedAt { get; set; }

        [Key]
        public Guid WorkshopId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string State { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        public string? Phone { get; set; }
        public string? Fax { get; set; }
        public string? Email { get; set; }
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankAccountHolderName { get; set; }
        public string? StripeConnectedAccountId { get; set; }
        public string? StripeOnboardingStatus { get; set; }
        public bool StripeChargesEnabled { get; set; }
        public bool StripePayoutsEnabled { get; set; }
        public DateTime? StripeLastSyncedAt { get; set; }

        [Required]
        public bool IsPanelWorkshop { get; set; } = true;

        [Required]
        public bool IsActive { get; set; } = true;

        public ICollection<WorkshopAppointmentEntity> Appointments { get; set; } = new List<WorkshopAppointmentEntity>();
        public ICollection<WorkshopRepairEstimateEntity> RepairEstimates { get; set; } = new List<WorkshopRepairEstimateEntity>();
        public ICollection<WorkshopPaymentEntity> Payments { get; set; } = new List<WorkshopPaymentEntity>();
        public ICollection<UserEntity> Users { get; set; } = new List<UserEntity>();
    }
}
