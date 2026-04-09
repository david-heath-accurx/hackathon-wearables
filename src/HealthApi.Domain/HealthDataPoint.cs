namespace HealthApi.Domain;

public record HealthDataPoint
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string UserId { get; init; }
    public required HealthMetricType MetricType { get; init; }
    public required double Value { get; init; }
    public required string Unit { get; init; }
    public required DateTimeOffset RecordedAt { get; init; }
    public string? DeviceId { get; init; }
    public string? DeviceModel { get; init; }
    public string? ExternalId { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public enum HealthMetricType
{
    HeartRate,
    Steps,
    ActiveCalories,
    RestingCalories,
    BloodOxygen,
    SleepDuration,
    StandHours,
    ExerciseMinutes,
    WorkoutDuration,
    RespiratoryRate,
    HeartRateVariability,
}
