using Motor.Claim.Domain.Entities;
using Motor.Claim.Domain.Enums;

namespace Motor.Claim.Application.Interfaces
{
    public interface IUserRepository : IGenericRepository<UserEntity>
    {
        Task<UserEntity?> GetByEmailAsync(string email);
        Task<UserEntity?> GetByIdWithWorkshopAsync(Guid userId);
        Task<List<UserEntity>> GetUsersAsync(UserRole? role, bool? isActive);
        Task<UserEntity?> GetByPasswordResetTokenHashAsync(string tokenHash);
    }
}
