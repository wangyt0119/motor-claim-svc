using Microsoft.EntityFrameworkCore;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.Domain.Entities;
using Motor.Claim.Infrastructure.Persistence.Context;

namespace Motor.Claim.Infrastructure.Persistence.Repositories
{
    public class UserRepository : GenericRepository<UserEntity>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<UserEntity?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
        }

        public async Task<UserEntity?> GetByPasswordResetTokenHashAsync(string tokenHash)
        {
            return await _context.Users.FirstOrDefaultAsync(x => x.PasswordResetTokenHash == tokenHash);
        }
    }
}
