using HealthApi.Domain;
using HealthApi.EntityFramework;
using Microsoft.AspNetCore.Mvc;

namespace HealthApi.Api.Controllers;

/// <summary>Admin health alert access — service-to-service, authenticated via API key</summary>
[ApiController]
[Route("admin/health-alerts")]
[ServiceFilter(typeof(ApiKeyAuthFilter))]
public class AdminHealthAlertsController(AlertStorage alerts, DeviceRegistrationStorage registrations) : ControllerBase
{
    /// <summary>Retrieve health alerts for a patient</summary>
    /// <remarks>
    /// Identify the patient either by patientIdentifier + dateOfBirth, or by forename + surname + dateOfBirth + odsCode.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(List<HealthAlertDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(typeof(string), 404)]
    public async Task<ActionResult<List<HealthAlertDto>>> Get(
        [FromQuery] string? patientIdentifier,
        [FromQuery] string? forename,
        [FromQuery] string? surname,
        [FromQuery] DateOnly dateOfBirth,
        [FromQuery] string? odsCode,
        CancellationToken ct
    )
    {
        Patient? patient;
        if (patientIdentifier is not null)
            patient = await registrations.FindPatientAsync(patientIdentifier, dateOfBirth, ct);
        else if (forename is not null && surname is not null && odsCode is not null)
            patient = await registrations.FindPatientByDemographicsAsync(forename, surname, dateOfBirth, odsCode, ct);
        else
            return BadRequest("Provide either patientIdentifier or forename + surname + odsCode.");

        if (patient is null)
            return NotFound("Patient not found.");

        var results = await alerts.GetAsync(patient.PatientIdentifier, ct);

        return results
            .Select(a => new HealthAlertDto(a.Id, a.Severity, a.Message, a.DetectedAt, a.AcknowledgedAt))
            .ToList();
    }
}

public record HealthAlertDto(
    Guid Id,
    string Severity,
    string Message,
    DateTimeOffset DetectedAt,
    DateTimeOffset? AcknowledgedAt
);
