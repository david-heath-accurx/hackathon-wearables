namespace HealthApi.Domain;

public record DeviceRegistration
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string PatientIdentifier { get; init; }
    public required DateOnly DateOfBirth { get; init; }
    public required string DeviceId { get; init; }
    public DateTimeOffset RegisteredAt { get; init; } = DateTimeOffset.UtcNow;
}
