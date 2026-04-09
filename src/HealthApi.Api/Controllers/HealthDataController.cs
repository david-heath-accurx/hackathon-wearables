using System.Security.Claims;
using HealthApi.Domain;
using HealthApi.EntityFramework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthApi.Api.Controllers;

/// <summary>Health data from wearable devices</summary>
[ApiController]
[Route("health-data")]
[Authorize]
public class HealthDataController(HealthDataStorage storage, DeviceRegistrationStorage registrations) : ControllerBase
{
    /// <summary>Submit health metric readings</summary>
    /// <remarks>
    /// Submit a batch of readings from a wearable device. The device must be registered.
    /// The patient identifier is sourced from the verified device registration, not the token.
    ///
    /// **Metric type values:**
    ///
    /// | Value | Metric | Typical unit |
    /// |-------|--------|-------------|
    /// | 0 | HeartRate | bpm |
    /// | 1 | Steps | steps |
    /// | 2 | ActiveCalories | kcal |
    /// | 3 | RestingCalories | kcal |
    /// | 4 | BloodOxygen | % |
    /// | 5 | SleepDuration | hours |
    /// | 6 | StandHours | hours |
    /// | 7 | ExerciseMinutes | minutes |
    /// | 8 | WorkoutDuration | minutes |
    /// | 9 | RespiratoryRate | breaths/min |
    /// | 10 | HeartRateVariability | ms |
    /// </remarks>
    /// <response code="200">Data stored successfully</response>
    /// <response code="401">Missing or invalid token</response>
    /// <response code="422">Device is not registered</response>
    [HttpPost]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(typeof(string), 422)]
    public async Task<IActionResult> Submit(
        [FromBody] SubmitHealthDataRequest request,
        CancellationToken ct
    )
    {
        var registration = await registrations.GetByDeviceIdAsync(request.DeviceId, ct);

        if (registration is null)
            return UnprocessableEntity("Device is not registered.");

        var points = request.DataPoints.Select(p => new HealthDataPoint
        {
            UserId = registration.PatientIdentifier,
            MetricType = p.MetricType,
            Value = p.Value,
            Unit = p.Unit,
            RecordedAt = p.RecordedAt,
            DeviceId = request.DeviceId,
            DeviceModel = request.DeviceModel,
        });

        await storage.SaveAsync(points, ct);

        return Ok();
    }

    /// <summary>Retrieve health data for the authenticated patient</summary>
    /// <remarks>Results are returned newest-first. All query parameters are optional.</remarks>
    /// <param name="metricType">Filter by metric type (see POST /health-data for values)</param>
    /// <param name="from">Start of time range (inclusive, ISO 8601)</param>
    /// <param name="to">End of time range (inclusive, ISO 8601)</param>
    /// <response code="200">List of health data points</response>
    /// <response code="401">Missing or invalid token</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<HealthDataPointDto>), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<List<HealthDataPointDto>>> Get(
        [FromQuery] HealthMetricType? metricType,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken ct
    )
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var results = await storage.GetAsync(userId, metricType, from, to, ct);

        return results
            .Select(p => new HealthDataPointDto(
                p.Id,
                p.MetricType,
                p.Value,
                p.Unit,
                p.RecordedAt,
                p.DeviceId,
                p.DeviceModel
            ))
            .ToList();
    }
}

/// <summary>Request body for POST /health-data</summary>
/// <param name="DataPoints">One or more metric readings to store</param>
/// <param name="DeviceId">ID of the registered device submitting the data</param>
/// <param name="DeviceModel">Human-readable device model name (e.g. "Apple Watch Series 9")</param>
public record SubmitHealthDataRequest(
    List<HealthDataPointInput> DataPoints,
    string DeviceId,
    string? DeviceModel
);

/// <summary>A single health metric reading</summary>
/// <param name="MetricType">Metric type (0–10, see endpoint description)</param>
/// <param name="Value">Numeric value of the reading</param>
/// <param name="Unit">Unit of measurement (e.g. "bpm", "steps", "%")</param>
/// <param name="RecordedAt">When the reading was recorded on the device (ISO 8601)</param>
public record HealthDataPointInput(
    HealthMetricType MetricType,
    double Value,
    string Unit,
    DateTimeOffset RecordedAt
);

/// <summary>A stored health data point</summary>
public record HealthDataPointDto(
    Guid Id,
    HealthMetricType MetricType,
    double Value,
    string Unit,
    DateTimeOffset RecordedAt,
    string? DeviceId,
    string? DeviceModel
);
