using Motor.Claim.Domain.Entities;

namespace Motor.Claim.Application.Interfaces
{
    public interface IWorkshopRepairEstimateRepository : IGenericRepository<WorkshopRepairEstimateEntity>
    {
        Task<WorkshopRepairEstimateEntity?> GetByClaimIdAsync(Guid claimId);
        Task<WorkshopRepairEstimateEntity?> GetByIdWithDetailsAsync(Guid estimateId);
        Task<List<WorkshopRepairEstimateEntity>> GetAllWithDetailsAsync();
        Task<List<WorkshopRepairEstimateEntity>> GetByWorkshopIdAsync(Guid workshopId);
    }
}
