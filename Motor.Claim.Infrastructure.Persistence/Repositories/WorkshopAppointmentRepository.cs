using Microsoft.EntityFrameworkCore;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.Domain.Entities;
using Motor.Claim.Infrastructure.Persistence.Context;

namespace Motor.Claim.Infrastructure.Persistence.Repositories
{
    public class WorkshopAppointmentRepository : GenericRepository<WorkshopAppointmentEntity>, IWorkshopAppointmentRepository
    {
        public WorkshopAppointmentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<WorkshopAppointmentEntity?> GetByClaimIdAsync(Guid claimId)
        {
            return await _context.WorkshopAppointments
                .Include(x => x.Workshop)
                .Include(x => x.Claim)
                .FirstOrDefaultAsync(x => x.ClaimId == claimId);
        }
    }
}
