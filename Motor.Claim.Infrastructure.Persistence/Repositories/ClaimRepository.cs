using Microsoft.EntityFrameworkCore;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.Domain.Entities;
using Motor.Claim.Domain.Enums;
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
                .Include(x => x.WorkshopAppointment)
                    .ThenInclude(x => x.Workshop)
                .Include(x => x.WorkshopRepairEstimate)
                    .ThenInclude(x => x.Workshop)
                .Include(x => x.WorkshopPayment)
                    .ThenInclude(x => x.Workshop)
                .ToListAsync();
        }

        public async Task<List<ClaimEntity>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Claims
                .Where(x => x.UserId == userId)
                .Include(x => x.Coverage)
                .Include(x => x.WorkshopAppointment)
                    .ThenInclude(x => x.Workshop)
                .Include(x => x.WorkshopRepairEstimate)
                    .ThenInclude(x => x.Workshop)
                .Include(x => x.WorkshopPayment)
                    .ThenInclude(x => x.Workshop)
                .ToListAsync();
        }

        public async Task<ClaimEntity?> GetByIdWithDetailsAsync(Guid claimId)
        {
            return await _context.Claims
                .Include(x => x.Coverage)
                .Include(x => x.WorkshopAppointment)
                    .ThenInclude(x => x.Workshop)
                .Include(x => x.WorkshopRepairEstimate)
                    .ThenInclude(x => x.Workshop)
                .Include(x => x.WorkshopPayment)
                    .ThenInclude(x => x.Workshop)
                .FirstOrDefaultAsync(x => x.ClaimId == claimId);
        }

        public async Task<List<Guid>> GetPendingStpValidationClaimIdsAsync(int take)
        {
            return await _context.Claims
                .AsNoTracking()
                .Where(x =>
                    x.Status == "Processing Validation" ||
                    x.Status == "Processing STP Validation" ||
                    x.ReviewStatus == "ProcessingStpValidation")
                .OrderBy(x => x.CreatedAt)
                .Select(x => x.ClaimId)
                .Take(take)
                .ToListAsync();
        }

        public async Task<bool> HasSubmittedClaimForCoverageSinceAsync(Guid coverageId, DateTime submittedSinceUtc)
        {
            return await _context.Claims.AnyAsync(x =>
                x.CoverageId == coverageId &&
                x.CreatedAt >= submittedSinceUtc);
        }

        public async Task<bool> HasActiveClaimForCoverageAsync(Guid coverageId)
        {
            return await _context.Claims.AnyAsync(x =>
                x.CoverageId == coverageId &&
                x.Status != "Approved" &&
                x.Status != "Rejected" &&
                x.Status != "Withdrawn");
        }

        public async Task<List<ClaimEntity>> GetApprovedClaimsByWorkshopIdAsync(Guid workshopId)
        {
            return await _context.Claims
                .Where(x =>
                    x.WorkshopAppointment != null &&
                    x.WorkshopAppointment.WorkshopId == workshopId &&
                    x.Status != "Withdrawn" &&
                    (x.ReviewStatus == "Approved" ||
                     x.STPStatus == StpStatus.AutoApproved ||
                     x.IsSTPApproved))
                .Include(x => x.Coverage)
                .Include(x => x.WorkshopAppointment)
                    .ThenInclude(x => x.Workshop)
                .Include(x => x.WorkshopRepairEstimate)
                    .ThenInclude(x => x.Workshop)
                .Include(x => x.WorkshopPayment)
                    .ThenInclude(x => x.Workshop)
                .ToListAsync();
        }
    }
}
