using Microsoft.EntityFrameworkCore;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.Domain.Entities;
using Motor.Claim.Infrastructure.Persistence.Context;

namespace Motor.Claim.Infrastructure.Persistence.Repositories
{
    public class WorkshopClaimLinkRequestRepository
        : GenericRepository<WorkshopClaimLinkRequestEntity>, IWorkshopClaimLinkRequestRepository
    {
        public WorkshopClaimLinkRequestRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<WorkshopClaimLinkRequestEntity?> GetByIdWithDetailsAsync(Guid requestId)
        {
            return await WithDetails()
                .FirstOrDefaultAsync(x => x.RequestId == requestId);
        }

        public async Task<WorkshopClaimLinkRequestEntity?> GetPendingByClaimIdAsync(Guid claimId)
        {
            return await WithDetails()
                .FirstOrDefaultAsync(x => x.ClaimId == claimId && x.Status == "Pending");
        }

        public async Task<List<WorkshopClaimLinkRequestEntity>> GetByCustomerIdAsync(Guid customerId)
        {
            return await WithDetails()
                .Where(x => x.Claim.UserId == customerId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<WorkshopClaimLinkRequestEntity>> GetByWorkshopIdAsync(Guid workshopId)
        {
            return await WithDetails()
                .Where(x => x.WorkshopId == workshopId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task AcceptAsync(
            WorkshopClaimLinkRequestEntity request,
            WorkshopAppointmentEntity appointment)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            _context.WorkshopAppointments.Add(appointment);
            request.Status = "Accepted";
            request.RespondedAt = DateTime.UtcNow;
            _context.WorkshopClaimLinkRequests.Update(request);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        private IQueryable<WorkshopClaimLinkRequestEntity> WithDetails()
        {
            return _context.WorkshopClaimLinkRequests
                .Include(x => x.Claim)
                    .ThenInclude(x => x.WorkshopAppointment)
                .Include(x => x.Claim)
                    .ThenInclude(x => x.WorkshopRepairEstimate)
                .Include(x => x.Workshop);
        }
    }
}
