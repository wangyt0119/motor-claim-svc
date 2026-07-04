using Motor.Claim.Domain.Entities;

namespace Motor.Claim.Application.Interfaces
{
    public interface ICoverageRepository : IGenericRepository<CoverageEntity>
    {
        Task<List<CoverageEntity>> GetByUserIdAsync(Guid userId);
        Task<List<CoverageEntity>> GetBasicByUserIdAsync(Guid userId);
        Task<List<CoverageEntity>> GetAllWithClaimsAsync();
    }
}
