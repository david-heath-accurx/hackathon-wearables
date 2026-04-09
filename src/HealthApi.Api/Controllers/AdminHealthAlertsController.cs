using HealthApi.Domain;
using HealthApi.EntityFramework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthApi.Api.Controllers;

/// <summary>Admin health alert access — service-to-service, authenticated via Key Vault signing key</summary>
[ApiController]
[Route("admin/health-alerts")]
[Authorize(Policy = "ServiceKey")]
public class AdminHealthAlertsController(AlertStorage alerts, DeviceRegistrationStorage registrations) : ControllerBase
{
    /// <summary>Retrieve health alerts for a patient</summary>
    /// <remarks>
    /// Returns all alerts raised by the monitoring agent for the given patient, newest first.
    /// The patient identifier and date of birth are used to verify the patient is registered.
    /// </remarks>
    /// <param name="patientIdentifier">Unique patient identifier (e.g. NHS number)</param>
    /// <param name="dateOfBirth">Patient date of birth (YYYY-MM-DD) — used to verify the patient</param>
    /// <response code="200">List of health alerts, newest first. Empty array if no alerts have been raised.</response>
    /// <response code="401">Missing or invalid service token</response>
    /// <response code="404">Patient identifier and date of birth not found</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<HealthAlertDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(typeof(string), 404)]
    public async Task<ActionResult<List<HealthAlertDto>>> Get(
        [FromQuery] string patientIdentifier,
        [FromQuery] DateOnly dateOfBirth,
        CancellationToken ct
    )
    {
        var patient = await registrations.FindPatientAsync(patientIdentifier, dateOfBirth, ct);

        if (patient is null)
            return NotFound("Patient not found.");

        var results = await alerts.GetAsync(patientIdentifier, ct);

        return results
            .Select(a => new HealthAlertDto(a.Id, a.Severity, a.Message, a.DetectedAt, a.AcknowledgedAt))
            .ToList();
    }
}

/// <summary>A health alert raised by the monitoring agent</summary>
public record HealthAlertDto(
    Guid Id,
    string Severity,
    string Message,
    DateTimeOffset DetectedAt,
    DateTimeOffset? AcknowledgedAt
);
