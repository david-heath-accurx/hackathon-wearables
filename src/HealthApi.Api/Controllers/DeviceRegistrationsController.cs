using HealthApi.EntityFramework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthApi.Api.Controllers;

[ApiController]
[Route("device-registrations")]
[Authorize]
public class DeviceRegistrationsController(DeviceRegistrationStorage storage) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Register(
        [FromBody] RegisterDeviceRequest request,
        CancellationToken ct
    )
    {
        var registered = await storage.RegisterAsync(request.PatientIdentifier, request.DateOfBirth, request.DeviceId, ct);

        if (!registered)
            return Conflict("This device is already registered.");

        return Ok();
    }

    [HttpDelete("{deviceId}")]
    public async Task<IActionResult> Deregister(string deviceId, CancellationToken ct)
    {
        var deregistered = await storage.DeregisterAsync(deviceId, ct);

        if (!deregistered)
            return NotFound("No registration found for this device.");

        return Ok();
    }

    [HttpDelete]
    public async Task<IActionResult> DeregisterAll(
        [FromQuery] string patientIdentifier,
        CancellationToken ct
    )
    {
        var deregistered = await storage.DeregisterAllAsync(patientIdentifier, ct);

        if (!deregistered)
            return NotFound("No registrations found for this patient.");

        return Ok();
    }
}

public record RegisterDeviceRequest(string PatientIdentifier, DateOnly DateOfBirth, string DeviceId);
