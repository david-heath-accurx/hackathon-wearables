namespace HealthApi.Domain;

public record Patient
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string PatientIdentifier { get; init; }
    public required string Forename { get; init; }
    public required string Surname { get; init; }
    public required DateOnly DateOfBirth { get; init; }
    public required string Postcode { get; init; }
    public required string PracticeOdsCode { get; init; }
    public DateTimeOffset RegisteredAt { get; init; } = DateTimeOffset.UtcNow;
}
