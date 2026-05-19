using Microsoft.EntityFrameworkCore;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.Domain.Entities;
using Motor.Claim.Infrastructure.Persistence.Context;

namespace Motor.Claim.Infrastructure.Persistence.Repositories
{
    public class WorkshopRepairEstimateRepository : GenericRepository<WorkshopRepairEstimateEntity>, IWorkshopRepairEstimateRepository
    {
        public WorkshopRepairEstimateRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<WorkshopRepairEstimateEntity?> GetByClaimIdAsync(Guid claimId)
        {
            return await _context.Set<WorkshopRepairEstimateEntity>()
                .Include(x => x.Workshop)
                .Include(x => x.Claim)
                    .ThenInclude(x => x.Coverage)
                .FirstOrDefaultAsync(x => x.ClaimId == claimId);
        }

        public async Task<WorkshopRepairEstimateEntity?> GetByIdWithDetailsAsync(Guid estimateId)
        {
            return await _context.Set<WorkshopRepairEstimateEntity>()
                .Include(x => x.Workshop)
                .Include(x => x.Claim)
                    .ThenInclude(x => x.Coverage)
                .FirstOrDefaultAsync(x => x.EstimateId == estimateId);
        }

        public async Task<List<WorkshopRepairEstimateEntity>> GetAllWithDetailsAsync()
        {
            return await _context.Set<WorkshopRepairEstimateEntity>()
                .Include(x => x.Workshop)
                .Include(x => x.Claim)
                    .ThenInclude(x => x.Coverage)
                .OrderByDescending(x => x.SubmittedAt)
                .ToListAsync();
        }

        public async Task<List<WorkshopRepairEstimateEntity>> GetByWorkshopIdAsync(Guid workshopId)
        {
            return await _context.Set<WorkshopRepairEstimateEntity>()
                .Include(x => x.Workshop)
                .Include(x => x.Claim)
                    .ThenInclude(x => x.Coverage)
                .Where(x => x.WorkshopId == workshopId)
                .OrderByDescending(x => x.SubmittedAt)
                .ToListAsync();
        }
    }
}
