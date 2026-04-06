using Motor.Claim.Domain.Entities;

namespace Motor.Claim.Application.Interfaces
{
    public interface IClaimRepository : IGenericRepository<ClaimEntity>
    {
        Task<List<ClaimEntity>> GetAllAsync();
        Task<List<ClaimEntity>> GetByUserIdAsync(Guid userId);
    }
}
