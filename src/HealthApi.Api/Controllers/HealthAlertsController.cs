using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HealthApi.EntityFramework;
using HealthApi.Functions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthApi.Api.Controllers;

/// <summary>Health alerts submitted directly by devices</summary>
[ApiController]
[Route("health-alerts")]
[Authorize]
public class HealthAlertsController(AlertStorage alerts, DeviceRegistrationStorage registrations, PatientInitiatedMessagingClient messaging) : ControllerBase
{
    /// <summary>Submit a health alert from a registered device</summary>
    /// <remarks>
    /// Creates a health alert for the authenticated patient and notifies their registered GP practice
    /// via patient-initiated messaging.
    /// </remarks>
    /// <response code="200">Alert created and practice notified</response>
    /// <response code="401">Missing or invalid token</response>
    /// <response code="404">Patient registration not found</response>
    [HttpPost]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Submit([FromBody] SubmitAlertRequest request, CancellationToken ct)
    {
        var patientIdentifier = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var patient = await registrations.FindPatientByIdentifierAsync(patientIdentifier, ct);
        if (patient is null)
            return NotFound("Patient registration not found.");

        await alerts.CreateAsync(patientIdentifier, "manual", request.Message, ct);

        await messaging.SendAlertAsync(patient, request.Message, ct);

        return Ok();
    }
}

/// <summary>Request body for POST /health-alerts</summary>
/// <param name="Message">Description of the health concern, up to 500 characters</param>
public record SubmitAlertRequest([MaxLength(500)] string Message);