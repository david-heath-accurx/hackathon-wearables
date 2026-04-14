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
    /// Identify the patient by either:
    /// - patientIdentifier + dateOfBirth
    /// - forename + surname + dateOfBirth (odsCode optional to narrow results)
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
        List<string> identifiers;

        if (patientIdentifier is not null)
        {
            var patient = await registrations.FindPatientAsync(patientIdentifier, dateOfBirth, ct);
            if (patient is null) return NotFound("Patient not found.");
            identifiers = [patient.PatientIdentifier];
        }
        else if (forename is not null && surname is not null)
        {
            List<Patient?> patients;
            if (odsCode is not null)
            {
                var single = await registrations.FindPatientByDemographicsAsync(forename, surname, dateOfBirth, odsCode, ct);
                patients = [single];
            }
            else
            {
                patients = await registrations.FindPatientsByNameAndDobAsync(forename, surname, dateOfBirth, ct);
            }

            identifiers = patients.Where(p => p is not null).Select(p => p!.PatientIdentifier).ToList();
            if (identifiers.Count == 0) return NotFound("Patient not found.");
        }
        else
        {
            return BadRequest("Provide either patientIdentifier or forename + surname.");
        }

        var results = new List<HealthAlertDto>();
        foreach (var id in identifiers)
        {
            var patientAlerts = await alerts.GetAsync(id, ct);
            results.AddRange(patientAlerts.Select(a => new HealthAlertDto(
                a.Id, a.Severity, a.Message, a.DetectedAt, a.AcknowledgedAt)));
        }

        return results.OrderByDescending(a => a.DetectedAt).ToList();
    }
}

public record HealthAlertDto(
    Guid Id,
    string Severity,
    string Message,
    DateTimeOffset DetectedAt,
    DateTimeOffset? AcknowledgedAt
);