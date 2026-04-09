namespace HealthApi.Domain;

public record HealthAlert
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid PatientId { get; init; }
    public Patient Patient { get; set; } = null!;
    public required string Severity { get; init; }
    public required string Message { get; init; }
    public DateTimeOffset DetectedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AcknowledgedAt { get; set; }
}
