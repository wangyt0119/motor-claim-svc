using Microsoft.EntityFrameworkCore;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.Domain.Entities;
using Motor.Claim.Infrastructure.Persistence.Context;

namespace Motor.Claim.Infrastructure.Persistence.Repositories
{
    public class WorkshopPaymentRepository : GenericRepository<WorkshopPaymentEntity>, IWorkshopPaymentRepository
    {
        public WorkshopPaymentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<WorkshopPaymentEntity?> GetByEstimateIdAsync(Guid estimateId)
        {
            return await _context.Set<WorkshopPaymentEntity>()
                .Include(x => x.Workshop)
                .Include(x => x.Estimate)
                .Include(x => x.Claim)
                .FirstOrDefaultAsync(x => x.EstimateId == estimateId);
        }

        public async Task<List<WorkshopPaymentEntity>> GetAllWithDetailsAsync()
        {
            return await _context.Set<WorkshopPaymentEntity>()
                .Include(x => x.Workshop)
                .Include(x => x.Estimate)
                .Include(x => x.Claim)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<WorkshopPaymentEntity>> GetByWorkshopIdAsync(Guid workshopId)
        {
            return await _context.Set<WorkshopPaymentEntity>()
                .Include(x => x.Workshop)
                .Include(x => x.Estimate)
                .Include(x => x.Claim)
                .Where(x => x.WorkshopId == workshopId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }
    }
}
