using HealthApi.Domain;
using HealthApi.EntityFramework;
using Microsoft.AspNetCore.Mvc;

namespace HealthApi.Api.Controllers;

/// <summary>Admin health data access — service-to-service, authenticated via API key</summary>
[ApiController]
[Route("admin/health-data")]
[ServiceFilter(typeof(ApiKeyAuthFilter))]
public class AdminHealthDataController(HealthDataStorage storage, DeviceRegistrationStorage registrations) : ControllerBase
{
    /// <summary>Retrieve health data for a patient</summary>
    /// <remarks>
    /// Identify the patient either by patientIdentifier + dateOfBirth, or by forename + surname + dateOfBirth + odsCode.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(List<HealthDataPointDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(typeof(string), 404)]
    public async Task<ActionResult<List<HealthDataPointDto>>> Get(
        [FromQuery] string? patientIdentifier,
        [FromQuery] string? forename,
        [FromQuery] string? surname,
        [FromQuery] DateOnly dateOfBirth,
        [FromQuery] string? odsCode,
        [FromQuery] HealthMetricType? metricType,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
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

        var results = await storage.GetAsync(patient.PatientIdentifier, metricType, from, to, ct);

        return results
            .Select(p => new HealthDataPointDto(
                p.Id, p.MetricType, p.MetricTypeName, p.Value, p.Unit,
                p.RecordedAt, p.DeviceRegistration.DeviceModel, p.ExternalId
            ))
            .ToList();
    }
}
