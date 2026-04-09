using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HealthApi.EntityFramework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace HealthApi.Api.Controllers;

[ApiController]
[Route("auth")]
[AllowAnonymous]
public class AuthController(IConfiguration config, DeviceRegistrationStorage storage) : ControllerBase
{
    [HttpPost("token")]
    public async Task<IActionResult> GetToken(
        [FromBody] TokenRequest request,
        CancellationToken ct
    )
    {
        var isRegistered = await storage.IsRegisteredAsync(
            request.PatientIdentifier,
            request.DateOfBirth,
            request.DeviceId,
            ct
        );

        if (!isRegistered)
            return Unauthorized("Device and patient registration not found.");

        var signingKey = new SymmetricSecurityKey(
            Convert.FromBase64String(config["Auth:SigningKey"]!)
        );

        var token = new JwtSecurityToken(
            issuer: config["Auth:Issuer"],
            audience: config["Auth:Audience"],
            claims: [
                new Claim(JwtRegisteredClaimNames.Sub, request.PatientIdentifier),
                new Claim("deviceId", request.DeviceId),
            ],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
        );

        return Ok(new
        {
            access_token = new JwtSecurityTokenHandler().WriteToken(token),
            expires_in = 3600,
            token_type = "Bearer",
        });
    }
}

public record TokenRequest(string PatientIdentifier, DateOnly DateOfBirth, string DeviceId);
