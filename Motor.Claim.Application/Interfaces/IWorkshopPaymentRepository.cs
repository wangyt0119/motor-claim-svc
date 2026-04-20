using Motor.Claim.Domain.Entities;

namespace Motor.Claim.Application.Interfaces
{
    public interface IWorkshopPaymentRepository : IGenericRepository<WorkshopPaymentEntity>
    {
        Task<WorkshopPaymentEntity?> GetByEstimateIdAsync(Guid estimateId);
        Task<List<WorkshopPaymentEntity>> GetAllWithDetailsAsync();
        Task<List<WorkshopPaymentEntity>> GetByWorkshopIdAsync(Guid workshopId);
    }
}
