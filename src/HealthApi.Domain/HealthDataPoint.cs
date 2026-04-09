namespace HealthApi.Domain;

public record HealthDataPoint
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid DeviceRegistrationId { get; init; }
    public DeviceRegistration DeviceRegistration { get; set; } = null!;
    public required HealthMetricType MetricType { get; init; }
    public required double Value { get; init; }
    public required string Unit { get; init; }
    public required DateTimeOffset RecordedAt { get; init; }
    public required string ExternalId { get; init; }
    public string MetricTypeName { get; init; } = null!;
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
