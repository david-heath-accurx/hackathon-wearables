namespace HealthApi.Domain;

public record DeviceRegistration
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid PatientId { get; init; }
    public Patient Patient { get; set; } = null!;
    public required string DeviceId { get; init; }
    public string? DeviceModel { get; set; }
    public DateTimeOffset RegisteredAt { get; init; } = DateTimeOffset.UtcNow;
    public ICollection<HealthDataPoint> HealthDataPoints { get; init; } = [];
}
