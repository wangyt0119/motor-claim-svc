using Motor.Claim.Domain.Entities;

namespace Motor.Claim.Application.Interfaces
{
    public interface ISystemActivityLogRepository : IGenericRepository<SystemActivityLogEntity>
    {
        Task<List<SystemActivityLogEntity>> GetFilteredAsync(
            DateTime? fromUtc,
            DateTime? toUtc,
            string? module,
            Guid? userId,
            string? userRole,
            int take);
    }
}
