namespace HealthApi.Domain;

public record Patient
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string PatientIdentifier { get; init; }
    public required DateOnly DateOfBirth { get; init; }
    public DateTimeOffset RegisteredAt { get; init; } = DateTimeOffset.UtcNow;
}
