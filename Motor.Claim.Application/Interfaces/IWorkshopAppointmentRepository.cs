using Motor.Claim.Domain.Entities;

namespace Motor.Claim.Application.Interfaces
{
    public interface IWorkshopAppointmentRepository : IGenericRepository<WorkshopAppointmentEntity>
    {
        Task<WorkshopAppointmentEntity?> GetByClaimIdAsync(Guid claimId);
        Task<WorkshopAppointmentEntity?> GetConflictingScheduledSlotAsync(
            Guid workshopId,
            DateTime preferredDate,
            TimeSpan timeSlotStart,
            TimeSpan timeSlotEnd,
            Guid? excludedClaimId = null);
        Task<List<WorkshopAppointmentEntity>> GetScheduledSlotsAsync(
            Guid workshopId,
            DateTime preferredDate,
            Guid? excludedClaimId = null);
    }
}
