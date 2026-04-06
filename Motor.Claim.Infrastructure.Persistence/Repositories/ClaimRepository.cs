using Microsoft.EntityFrameworkCore;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.Domain.Entities;
using Motor.Claim.Infrastructure.Persistence.Context;

namespace Motor.Claim.Infrastructure.Persistence.Repositories
{
    public class ClaimRepository : GenericRepository<ClaimEntity>, IClaimRepository
    {
        public ClaimRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<ClaimEntity>> GetAllAsync()
        {
            return await _context.Claims
                .Include(x => x.Coverage)
                .ToListAsync();
        }

        public async Task<List<ClaimEntity>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Claims
                .Where(x => x.UserId == userId)
                .Include(x => x.Coverage)
                .ToListAsync();
        }
    }
}
