using Microsoft.EntityFrameworkCore;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.Domain.Entities;
using Motor.Claim.Infrastructure.Persistence.Context;

namespace Motor.Claim.Infrastructure.Persistence.Repositories
{
    public class CoverageRepository : GenericRepository<CoverageEntity>, ICoverageRepository
    {
        public CoverageRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<CoverageEntity>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Coverages
                .Where(x => x.UserId == userId)
                .Include(x => x.Claims)
                    .ThenInclude(x => x.WorkshopAppointment)
                        .ThenInclude(x => x.Workshop)
                .Include(x => x.Claims)
                    .ThenInclude(x => x.WorkshopRepairEstimate)
                        .ThenInclude(x => x.Workshop)
                .Include(x => x.Claims)
                    .ThenInclude(x => x.WorkshopPayment)
                        .ThenInclude(x => x.Workshop)
                .ToListAsync();
        }

        public async Task<List<CoverageEntity>> GetAllWithClaimsAsync()
        {
            return await _context.Coverages
                .Include(x => x.Claims)
                    .ThenInclude(x => x.WorkshopAppointment)
                        .ThenInclude(x => x.Workshop)
                .Include(x => x.Claims)
                    .ThenInclude(x => x.WorkshopRepairEstimate)
                        .ThenInclude(x => x.Workshop)
                .Include(x => x.Claims)
                    .ThenInclude(x => x.WorkshopPayment)
                        .ThenInclude(x => x.Workshop)
                .ToListAsync();
        }
    }
}
