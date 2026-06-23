using Motor.Claim.Domain.Entities;

namespace Motor.Claim.Application.Interfaces
{
    public interface IWorkshopClaimLinkRequestRepository : IGenericRepository<WorkshopClaimLinkRequestEntity>
    {
        Task<WorkshopClaimLinkRequestEntity?> GetByIdWithDetailsAsync(Guid requestId);
        Task<WorkshopClaimLinkRequestEntity?> GetPendingByClaimIdAsync(Guid claimId);
        Task<List<WorkshopClaimLinkRequestEntity>> GetByCustomerIdAsync(Guid customerId);
        Task<List<WorkshopClaimLinkRequestEntity>> GetByWorkshopIdAsync(Guid workshopId);
        Task AcceptAsync(WorkshopClaimLinkRequestEntity request, WorkshopAppointmentEntity appointment);
    }
}
