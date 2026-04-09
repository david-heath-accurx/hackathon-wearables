using HealthApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace HealthApi.EntityFramework;

public class HealthApiDbContext(DbContextOptions<HealthApiDbContext> options) : DbContext(options)
{
    public DbSet<HealthDataPoint> HealthDataPoints => Set<HealthDataPoint>();
    public DbSet<DeviceRegistration> DeviceRegistrations => Set<DeviceRegistration>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<HealthAlert> HealthAlerts => Set<HealthAlert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HealthDataPoint>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.DeviceRegistrationId, e.MetricType, e.RecordedAt });
            entity.Property(e => e.Unit).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ExternalId).HasMaxLength(256).IsRequired();
            entity.Property(e => e.MetricTypeName)
                .HasMaxLength(50)
                .HasComputedColumnSql("""
                    CASE [MetricType]
                      WHEN 0 THEN N'HeartRate'
                      WHEN 1 THEN N'Steps'
                      WHEN 2 THEN N'ActiveCalories'
                      WHEN 3 THEN N'RestingCalories'
                      WHEN 4 THEN N'BloodOxygen'
                      WHEN 5 THEN N'SleepDuration'
                      WHEN 6 THEN N'StandHours'
                      WHEN 7 THEN N'ExerciseMinutes'
                      WHEN 8 THEN N'WorkoutDuration'
                      WHEN 9 THEN N'RespiratoryRate'
                      WHEN 10 THEN N'HeartRateVariability'
                      ELSE CAST([MetricType] AS NVARCHAR(50))
                    END
                    """, stored: true);
            entity.HasIndex(e => new { e.DeviceRegistrationId, e.ExternalId })
                .IsUnique();
            entity.HasOne(e => e.DeviceRegistration)
                .WithMany(r => r.HealthDataPoints)
                .HasForeignKey(e => e.DeviceRegistrationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PatientIdentifier).IsUnique();
            entity.Property(e => e.PatientIdentifier).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PracticeOdsCode).HasMaxLength(10).IsRequired();
        });

        modelBuilder.Entity<DeviceRegistration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DeviceId).IsUnique();
            entity.Property(e => e.DeviceId).HasMaxLength(256).IsRequired();
            entity.Property(e => e.DeviceModel).HasMaxLength(256);
            entity.HasOne(e => e.Patient)
                .WithMany()
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HealthAlert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PatientId, e.DetectedAt });
            entity.Property(e => e.Severity).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(2000).IsRequired();
            entity.HasOne(e => e.Patient)
                .WithMany()
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
