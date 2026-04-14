using HealthApi.EntityFramework;
using Microsoft.AspNetCore.Mvc;

namespace HealthApi.Api.Controllers;

/// <summary>Admin patient lookup — service-to-service, authenticated via API key</summary>
[ApiController]
[Route("admin/patients")]
[ServiceFilter(typeof(ApiKeyAuthFilter))]
public class AdminPatientsController(DeviceRegistrationStorage registrations) : ControllerBase
{
    /// <summary>Look up patients by forename, surname and date of birth</summary>
    /// <remarks>
    /// Returns all patients matching the supplied demographics. Use the returned
    /// patientIdentifier to query health data or alerts via the other admin endpoints.
    /// </remarks>
    /// <response code="200">List of matching patients (may be empty)</response>
    /// <response code="401">Missing or invalid API key</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<PatientSummaryDto>), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<List<PatientSummaryDto>>> Get(
        [FromQuery] string forename,
        [FromQuery] string surname,
        [FromQuery] DateOnly dateOfBirth,
        CancellationToken ct
    )
    {
        var patients = await registrations.FindPatientsByNameAndDobAsync(forename, surname, dateOfBirth, ct);

        return patients
            .Select(p => new PatientSummaryDto(
                p.PatientIdentifier,
                p.Forename,
                p.Surname,
                p.DateOfBirth,
                p.Postcode,
                p.PracticeOdsCode,
                p.RegisteredAt))
            .ToList();
    }
}

public record PatientSummaryDto(
    string PatientIdentifier,
    string Forename,
    string Surname,
    DateOnly DateOfBirth,
    string Postcode,
    string PracticeOdsCode,
    DateTimeOffset RegisteredAt
);
