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
        public DbSet<WorkshopEntity> Workshops { get; set; }
        public DbSet<WorkshopAppointmentEntity> WorkshopAppointments { get; set; }
        public DbSet<WorkshopRepairEstimateEntity> WorkshopRepairEstimates { get; set; }
        public DbSet<WorkshopPaymentEntity> WorkshopPayments { get; set; }
        public DbSet<SystemActivityLogEntity> SystemActivityLogs { get; set; }

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

            modelBuilder.Entity<WorkshopEntity>()
                .Property(x => x.Name)
                .HasMaxLength(200);

            modelBuilder.Entity<WorkshopEntity>()
                .Property(x => x.State)
                .HasMaxLength(100);

            modelBuilder.Entity<WorkshopEntity>()
                .Property(x => x.StripeConnectedAccountId)
                .HasMaxLength(255);

            modelBuilder.Entity<WorkshopEntity>()
                .Property(x => x.StripeOnboardingStatus)
                .HasMaxLength(50);

            modelBuilder.Entity<WorkshopAppointmentEntity>()
                .HasOne(x => x.Claim)
                .WithOne(x => x.WorkshopAppointment)
                .HasForeignKey<WorkshopAppointmentEntity>(x => x.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WorkshopAppointmentEntity>()
                .HasOne(x => x.Workshop)
                .WithMany(x => x.Appointments)
                .HasForeignKey(x => x.WorkshopId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkshopRepairEstimateEntity>()
                .HasKey(x => x.EstimateId);

            modelBuilder.Entity<WorkshopRepairEstimateEntity>()
                .HasOne(x => x.Claim)
                .WithOne(x => x.WorkshopRepairEstimate)
                .HasForeignKey<WorkshopRepairEstimateEntity>(x => x.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WorkshopRepairEstimateEntity>()
                .HasOne(x => x.Workshop)
                .WithMany(x => x.RepairEstimates)
                .HasForeignKey(x => x.WorkshopId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkshopRepairEstimateEntity>()
                .Property(x => x.TotalAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<WorkshopPaymentEntity>()
                .HasKey(x => x.PaymentId);

            modelBuilder.Entity<WorkshopPaymentEntity>()
                .HasOne(x => x.Estimate)
                .WithOne(x => x.Payment)
                .HasForeignKey<WorkshopPaymentEntity>(x => x.EstimateId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkshopPaymentEntity>()
                .HasOne(x => x.Claim)
                .WithOne(x => x.WorkshopPayment)
                .HasForeignKey<WorkshopPaymentEntity>(x => x.ClaimId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkshopPaymentEntity>()
                .HasOne(x => x.Workshop)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.WorkshopId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkshopPaymentEntity>()
                .Property(x => x.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<WorkshopPaymentEntity>()
                .Property(x => x.Currency)
                .HasMaxLength(10);

            modelBuilder.Entity<WorkshopPaymentEntity>()
                .Property(x => x.Status)
                .HasMaxLength(50);

            modelBuilder.Entity<WorkshopPaymentEntity>()
                .Property(x => x.Provider)
                .HasMaxLength(50);

            modelBuilder.Entity<WorkshopPaymentEntity>()
                .Property(x => x.ApprovalSource)
                .HasMaxLength(50);

            modelBuilder.Entity<UserEntity>()
                .HasOne(x => x.Workshop)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.WorkshopId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SystemActivityLogEntity>()
                .Property(x => x.Module)
                .HasMaxLength(100);

            modelBuilder.Entity<SystemActivityLogEntity>()
                .Property(x => x.Action)
                .HasMaxLength(50);

            modelBuilder.Entity<SystemActivityLogEntity>()
                .Property(x => x.HttpMethod)
                .HasMaxLength(10);

            modelBuilder.Entity<SystemActivityLogEntity>()
                .Property(x => x.UserRole)
                .HasMaxLength(50);

            modelBuilder.Entity<SystemActivityLogEntity>()
                .HasIndex(x => x.CreatedAt);
        }
    }
}
