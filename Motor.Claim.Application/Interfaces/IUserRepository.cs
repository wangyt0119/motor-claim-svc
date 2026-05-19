using Motor.Claim.Domain.Entities;

namespace Motor.Claim.Application.Interfaces
{
    public interface IUserRepository : IGenericRepository<UserEntity>
    {
        Task<UserEntity?> GetByEmailAsync(string email);
        Task<UserEntity?> GetByPasswordResetTokenHashAsync(string tokenHash);
    }
}
