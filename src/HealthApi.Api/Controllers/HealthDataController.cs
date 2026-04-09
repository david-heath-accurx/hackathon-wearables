using System.Security.Claims;
using HealthApi.Domain;
using HealthApi.EntityFramework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthApi.Api.Controllers;

[ApiController]
[Route("health-data")]
[Authorize]
public class HealthDataController(HealthDataStorage storage, DeviceRegistrationStorage registrations) : ControllerBase
{
    [HttpPost]
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

    [HttpGet]
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

public record SubmitHealthDataRequest(
    List<HealthDataPointInput> DataPoints,
    string DeviceId,
    string? DeviceModel
);

public record HealthDataPointInput(
    HealthMetricType MetricType,
    double Value,
    string Unit,
    DateTimeOffset RecordedAt
);

public record HealthDataPointDto(
    Guid Id,
    HealthMetricType MetricType,
    double Value,
    string Unit,
    DateTimeOffset RecordedAt,
    string? DeviceId,
    string? DeviceModel
);
