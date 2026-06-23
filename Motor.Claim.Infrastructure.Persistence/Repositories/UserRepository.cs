using Microsoft.EntityFrameworkCore;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.Domain.Entities;
using Motor.Claim.Domain.Enums;
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

        public async Task<UserEntity?> GetByIdWithWorkshopAsync(Guid userId)
        {
            return await _context.Users
                .Include(x => x.Workshop)
                .FirstOrDefaultAsync(x => x.UserId == userId);
        }

        public async Task<List<UserEntity>> GetUsersAsync(UserRole? role, bool? isActive)
        {
            var query = _context.Users
                .Include(x => x.Workshop)
                .AsQueryable();

            if (role.HasValue)
            {
                query = query.Where(x => x.Role == role.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            return await query
                .OrderBy(x => x.Role)
                .ThenBy(x => x.FullName)
                .ToListAsync();
        }

        public async Task<UserEntity?> GetByPasswordResetTokenHashAsync(string tokenHash)
        {
            return await _context.Users.FirstOrDefaultAsync(x => x.PasswordResetTokenHash == tokenHash);
        }
    }
}
