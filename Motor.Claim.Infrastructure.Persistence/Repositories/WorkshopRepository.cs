using Microsoft.EntityFrameworkCore;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.Domain.Entities;
using Motor.Claim.Infrastructure.Persistence.Context;

namespace Motor.Claim.Infrastructure.Persistence.Repositories
{
    public class WorkshopRepository : GenericRepository<WorkshopEntity>, IWorkshopRepository
    {
        public WorkshopRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<string>> GetActivePanelStatesAsync()
        {
            return await _context.Workshops
                .Where(x => x.IsActive && x.IsPanelWorkshop)
                .Select(x => x.State.Trim())
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();
        }

        public async Task<List<WorkshopEntity>> GetActivePanelWorkshopsByStateAsync(string state)
        {
            var normalizedState = state.Trim();

            return await _context.Workshops
                .Where(x => x.IsActive && x.IsPanelWorkshop && x.State == normalizedState)
                .OrderBy(x => x.Name)
                .ToListAsync();
        }
    }
}
