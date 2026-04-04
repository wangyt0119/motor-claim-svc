using Microsoft.EntityFrameworkCore;
using Motor.Claim.Domain.Entities;

namespace Motor.Claim.Infrastructure.Persistence.Context
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserEntity> Users { get; set; }
        public DbSet<CoverageEntity> Coverages { get; set; }
        public DbSet<ClaimEntity> Claims { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CoverageEntity>()
                .HasOne(c => c.User)
                .WithMany(u => u.Coverages)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClaimEntity>()
                .HasOne(c => c.User)
                .WithMany(u => u.Claims)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClaimEntity>()
                .HasOne(c => c.Coverage)
                .WithMany(cv => cv.Claims)
                .HasForeignKey(c => c.CoverageId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}