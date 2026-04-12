using Motor.Claim.Domain.Entities;

namespace Motor.Claim.Application.Interfaces
{
    public interface IWorkshopAppointmentRepository : IGenericRepository<WorkshopAppointmentEntity>
    {
        Task<WorkshopAppointmentEntity?> GetByClaimIdAsync(Guid claimId);
    }
}
