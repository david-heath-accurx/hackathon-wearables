using HealthApi.EntityFramework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthApi.Api.Controllers;

/// <summary>Device registration — records patient consent to share health data</summary>
[ApiController]
[Route("device-registrations")]
public class DeviceRegistrationsController(DeviceRegistrationStorage storage) : ControllerBase
{
    /// <summary>Register a device for a patient</summary>
    /// <remarks>
    /// Called when a patient gives consent to share their health data.
    /// The device ID uniquely identifies the patient's mobile phone.
    /// </remarks>
    /// <response code="200">Device registered successfully</response>
    /// <response code="409">This device ID is already registered</response>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(200)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterDeviceRequest request,
        CancellationToken ct
    )
    {
        var registered = await storage.RegisterAsync(request.PatientIdentifier, request.DateOfBirth, request.PracticeOdsCode, request.DeviceId, ct);

        if (!registered)
            return Conflict("This device is already registered.");

        return Ok();
    }

    /// <summary>Deregister a single device</summary>
    /// <remarks>
    /// Removes the device registration and deletes all health data associated with that device.
    /// </remarks>
    /// <param name="deviceId">The device ID to deregister</param>
    /// <response code="200">Device deregistered and its health data deleted</response>
    /// <response code="401">Missing or invalid token</response>
    /// <response code="404">No registration found for this device</response>
    [HttpDelete("{deviceId}")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Deregister(string deviceId, CancellationToken ct)
    {
        var deregistered = await storage.DeregisterAsync(deviceId, ct);

        if (!deregistered)
            return NotFound("No registration found for this device.");

        return Ok();
    }

    /// <summary>Deregister all devices for a patient</summary>
    /// <remarks>
    /// Removes all device registrations for the patient and deletes all their associated health data.
    /// Use when a patient withdraws consent entirely.
    /// </remarks>
    /// <param name="patientIdentifier">The patient identifier whose registrations should be removed</param>
    /// <response code="200">All devices deregistered and health data deleted</response>
    /// <response code="401">Missing or invalid token</response>
    /// <response code="404">No registrations found for this patient</response>
    [HttpDelete]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
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

/// <summary>Request body for POST /device-registrations</summary>
/// <param name="PatientIdentifier">Unique patient identifier, up to 100 characters (e.g. NHS number)</param>
/// <param name="DateOfBirth">Patient date of birth (YYYY-MM-DD)</param>
/// <param name="PracticeOdsCode">ODS code of the patient's registered GP practice (e.g. "A81001")</param>
/// <param name="DeviceId">Unique identifier for the patient's mobile device</param>
public record RegisterDeviceRequest(string PatientIdentifier, DateOnly DateOfBirth, string PracticeOdsCode, string DeviceId);
