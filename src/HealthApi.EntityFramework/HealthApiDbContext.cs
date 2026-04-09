using HealthApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace HealthApi.EntityFramework;

public class HealthApiDbContext(DbContextOptions<HealthApiDbContext> options) : DbContext(options)
{
    public DbSet<HealthDataPoint> HealthDataPoints => Set<HealthDataPoint>();
    public DbSet<DeviceRegistration> DeviceRegistrations => Set<DeviceRegistration>();
    public DbSet<Patient> Patients => Set<Patient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HealthDataPoint>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.MetricType, e.RecordedAt });
            entity.Property(e => e.UserId).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Unit).HasMaxLength(50).IsRequired();
            entity.Property(e => e.DeviceId).HasMaxLength(256);
            entity.Property(e => e.DeviceModel).HasMaxLength(256);
            entity.Property(e => e.ExternalId).HasMaxLength(256);
            entity.HasIndex(e => new { e.DeviceId, e.ExternalId })
                .IsUnique()
                .HasFilter("[ExternalId] IS NOT NULL");
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PatientIdentifier).IsUnique();
            entity.Property(e => e.PatientIdentifier).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<DeviceRegistration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DeviceId).IsUnique();
            entity.Property(e => e.DeviceId).HasMaxLength(256).IsRequired();
            entity.HasOne(e => e.Patient)
                .WithMany()
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
