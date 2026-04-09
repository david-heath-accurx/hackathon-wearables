namespace HealthApi.Domain;

public record DeviceRegistration
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid PatientId { get; init; }
    public Patient Patient { get; set; } = null!;
    public required string DeviceId { get; init; }
    public DateTimeOffset RegisteredAt { get; init; } = DateTimeOffset.UtcNow;
}
