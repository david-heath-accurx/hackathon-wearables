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
    /// <summary>Retrieve health data for a patient</summary>
    /// <remarks>
    /// Identify the patient by either:
    /// - patientIdentifier + dateOfBirth
    /// - forename + surname + dateOfBirth (odsCode optional to narrow results)
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
        List<string> identifiers;

        if (patientIdentifier is not null)
        {
            var patient = await registrations.FindPatientAsync(patientIdentifier, dateOfBirth, ct);
            if (patient is null) return NotFound("Patient not found.");
            identifiers = [patient.PatientIdentifier];
        }
        else if (forename is not null && surname is not null)
        {
            var patients = odsCode is not null
                ? [await registrations.FindPatientByDemographicsAsync(forename, surname, dateOfBirth, odsCode, ct)]
                : await registrations.FindPatientsByNameAndDobAsync(forename, surname, dateOfBirth, ct);

            identifiers = patients.Where(p => p is not null).Select(p => p!.PatientIdentifier).ToList();
            if (identifiers.Count == 0) return NotFound("Patient not found.");
        }
        else
        {
            return BadRequest("Provide either patientIdentifier or forename + surname.");
        }

        var results = new List<HealthDataPointDto>();
        foreach (var id in identifiers)
        {
            var points = await storage.GetAsync(id, metricType, from, to, ct);
            results.AddRange(points.Select(p => new HealthDataPointDto(
                p.Id, p.MetricType, p.MetricTypeName, p.Value, p.Unit,
                p.RecordedAt, p.DeviceRegistration.DeviceModel, p.ExternalId)));
        }

        return results.OrderByDescending(p => p.RecordedAt).ToList();
    }
}