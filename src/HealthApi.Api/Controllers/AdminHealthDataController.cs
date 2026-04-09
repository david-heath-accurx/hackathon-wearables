using HealthApi.Domain;
using HealthApi.EntityFramework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthApi.Api.Controllers;

/// <summary>Admin health data access — service-to-service, authenticated via Key Vault signing key</summary>
[ApiController]
[Route("admin/health-data")]
[Authorize(Policy = "ServiceKey")]
public class AdminHealthDataController(HealthDataStorage storage, DeviceRegistrationStorage registrations) : ControllerBase
{
    /// <summary>Retrieve health data for a patient by identifier and date of birth</summary>
    /// <remarks>
    /// Authenticates via a JWT signed with the service signing key stored in Azure Key Vault.
    /// The patient identifier and date of birth are used to verify the patient is registered
    /// before returning their data. All query filters are optional.
    /// </remarks>
    /// <param name="patientIdentifier">Unique patient identifier (e.g. NHS number)</param>
    /// <param name="dateOfBirth">Patient date of birth (YYYY-MM-DD) — used to verify the patient</param>
    /// <param name="metricType">Filter by metric type (see POST /health-data for values)</param>
    /// <param name="from">Start of time range (inclusive, ISO 8601)</param>
    /// <param name="to">End of time range (inclusive, ISO 8601)</param>
    /// <response code="200">List of health data points for the patient</response>
    /// <response code="401">Missing or invalid service token</response>
    /// <response code="404">No registered patient found matching these details</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<HealthDataPointDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(typeof(string), 404)]
    public async Task<ActionResult<List<HealthDataPointDto>>> Get(
        [FromQuery] string patientIdentifier,
        [FromQuery] DateOnly dateOfBirth,
        [FromQuery] HealthMetricType? metricType,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken ct
    )
    {
        var patientExists = await registrations.PatientExistsAsync(patientIdentifier, dateOfBirth, ct);

        if (!patientExists)
            return NotFound("No registered patient found matching these details.");

        var results = await storage.GetAsync(patientIdentifier, metricType, from, to, ct);

        return results
            .Select(p => new HealthDataPointDto(
                p.Id,
                p.MetricType,
                p.Value,
                p.Unit,
                p.RecordedAt,
                p.DeviceId,
                p.DeviceModel,
                p.ExternalId
            ))
            .ToList();
    }
}
