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

        public async Task<WorkshopAppointmentEntity?> GetConflictingScheduledSlotAsync(
            Guid workshopId,
            DateTime preferredDate,
            TimeSpan timeSlotStart,
            TimeSpan timeSlotEnd,
            Guid? excludedClaimId = null)
        {
            return await _context.WorkshopAppointments
                .Include(x => x.Workshop)
                .Include(x => x.Claim)
                .FirstOrDefaultAsync(x =>
                    x.WorkshopId == workshopId &&
                    x.PreferredDate.Date == preferredDate.Date &&
                    x.AssignmentType == "ScheduledAppointment" &&
                    x.Status != "Cancelled" &&
                    (!excludedClaimId.HasValue || x.ClaimId != excludedClaimId.Value) &&
                    x.TimeSlotStart < timeSlotEnd &&
                    timeSlotStart < x.TimeSlotEnd);
        }

        public async Task<List<WorkshopAppointmentEntity>> GetScheduledSlotsAsync(
            Guid workshopId,
            DateTime preferredDate,
            Guid? excludedClaimId = null)
        {
            return await _context.WorkshopAppointments
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.PreferredDate.Date == preferredDate.Date &&
                    x.AssignmentType == "ScheduledAppointment" &&
                    x.Status != "Cancelled" &&
                    (!excludedClaimId.HasValue || x.ClaimId != excludedClaimId.Value))
                .OrderBy(x => x.TimeSlotStart)
                .ToListAsync();
        }
    }
}
