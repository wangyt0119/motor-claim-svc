using Microsoft.EntityFrameworkCore;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.Domain.Entities;
using Motor.Claim.Infrastructure.Persistence.Context;

namespace Motor.Claim.Infrastructure.Persistence.Repositories
{
    public class SystemActivityLogRepository : GenericRepository<SystemActivityLogEntity>, ISystemActivityLogRepository
    {
        public SystemActivityLogRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<SystemActivityLogEntity>> GetFilteredAsync(
            DateTime? fromUtc,
            DateTime? toUtc,
            string? module,
            Guid? userId,
            string? userRole,
            int take)
        {
            var query = _context.Set<SystemActivityLogEntity>().AsQueryable();

            if (fromUtc.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= fromUtc.Value);
            }

            if (toUtc.HasValue)
            {
                query = query.Where(x => x.CreatedAt <= toUtc.Value);
            }

            if (!string.IsNullOrWhiteSpace(module))
            {
                var normalizedModule = module.Trim();
                query = query.Where(x => x.Module == normalizedModule);
            }

            if (userId.HasValue)
            {
                query = query.Where(x => x.UserId == userId.Value);
            }

            if (!string.IsNullOrWhiteSpace(userRole))
            {
                var normalizedRole = userRole.Trim();
                query = query.Where(x => x.UserRole == normalizedRole);
            }

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .Take(Math.Clamp(take, 1, 5000))
                .ToListAsync();
        }
    }
}
