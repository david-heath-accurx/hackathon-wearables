using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HealthApi.EntityFramework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace HealthApi.Api.Controllers;

/// <summary>Authentication</summary>
[ApiController]
[Route("auth")]
[AllowAnonymous]
public class AuthController(IConfiguration config, DeviceRegistrationStorage storage) : ControllerBase
{
    /// <summary>Get an access token</summary>
    /// <remarks>
    /// Validates that the device ID, patient name, and date of birth match an active registration.
    /// Returns a signed JWT valid for 1 hour. Use it as a Bearer token on all other endpoints.
    /// </remarks>
    /// <response code="200">JWT access token</response>
    /// <response code="401">No matching device registration found</response>
    [HttpPost("token")]
    [ProducesResponseType(typeof(TokenResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetToken(
        [FromBody] TokenRequest request,
        CancellationToken ct
    )
    {
        var patientIdentifier = await storage.GetPatientIdentifierAsync(
            request.Forename,
            request.Surname,
            request.DateOfBirth,
            request.DeviceId,
            ct
        );

        if (patientIdentifier is null)
            return Unauthorized("Device and patient registration not found.");

        var signingKey = new SymmetricSecurityKey(
            Convert.FromBase64String(config["Auth:SigningKey"]!)
        );

        var token = new JwtSecurityToken(
            issuer: config["Auth:Issuer"],
            audience: config["Auth:Audience"],
            claims: [
                new Claim(JwtRegisteredClaimNames.Sub, patientIdentifier),
                new Claim("deviceId", request.DeviceId),
            ],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
        );

        return Ok(new TokenResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresIn: 3600,
            TokenType: "Bearer"
        ));
    }
}

/// <summary>Request body for POST /auth/token</summary>
/// <param name="Forename">Patient first name</param>
/// <param name="Surname">Patient last name</param>
/// <param name="DateOfBirth">Patient date of birth (YYYY-MM-DD)</param>
/// <param name="DeviceId">Unique identifier for the patient's mobile device</param>
public record TokenRequest(string Forename, string Surname, DateOnly DateOfBirth, string DeviceId);

/// <summary>JWT token response</summary>
public record TokenResponse(
    string access_token,
    int ExpiresIn,
    string TokenType
);
