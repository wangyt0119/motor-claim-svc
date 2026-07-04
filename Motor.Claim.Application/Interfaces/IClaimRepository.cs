using Motor.Claim.Domain.Entities;

namespace Motor.Claim.Application.Interfaces
{
    public interface IClaimRepository : IGenericRepository<ClaimEntity>
    {
        Task<List<ClaimEntity>> GetAllAsync();
        Task<List<ClaimEntity>> GetByUserIdAsync(Guid userId);
        Task<List<ClaimEntity>> GetApprovedClaimsByWorkshopIdAsync(Guid workshopId);
        Task<List<Guid>> GetPendingStpValidationClaimIdsAsync(int take);
        Task<ClaimEntity?> GetByIdWithDetailsAsync(Guid claimId);
        Task UpdateStpValidationResultAsync(ClaimEntity claim);
        Task UpdateOfficerDecisionAsync(ClaimEntity claim);
        Task<bool> HasActiveClaimForCoverageAsync(Guid coverageId);
        Task<bool> HasSubmittedClaimForCoverageSinceAsync(Guid coverageId, DateTime submittedSinceUtc);
    }
}
