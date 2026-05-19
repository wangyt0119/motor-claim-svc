using Motor.Claim.Domain.Entities;

namespace Motor.Claim.Application.Interfaces
{
    public interface IClaimRepository : IGenericRepository<ClaimEntity>
    {
        Task<List<ClaimEntity>> GetAllAsync();
        Task<List<ClaimEntity>> GetByUserIdAsync(Guid userId);
        Task<List<ClaimEntity>> GetApprovedClaimsByWorkshopIdAsync(Guid workshopId);
        Task<ClaimEntity?> GetByIdWithDetailsAsync(Guid claimId);
        Task<bool> HasSubmittedClaimForCoverageSinceAsync(Guid coverageId, DateTime submittedSinceUtc);
    }
}
