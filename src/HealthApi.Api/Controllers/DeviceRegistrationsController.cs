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
        var registered = await storage.RegisterAsync(request.PatientId, request.DeviceId, ct);

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
}

public record RegisterDeviceRequest(int PatientId, string DeviceId);
