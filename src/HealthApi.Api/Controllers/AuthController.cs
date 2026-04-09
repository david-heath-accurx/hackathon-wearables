using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthApi.Api.Controllers;

[ApiController]
[Route("auth")]
[AllowAnonymous]
public class AuthController(IConfiguration config, IHttpClientFactory httpClientFactory) : ControllerBase
{
    [HttpPost("token")]
    public async Task<IActionResult> GetToken(
        [FromBody] TokenRequest request,
        CancellationToken ct
    )
    {
        var tenantId = config["Auth:TenantId"];
        var apiAppId = config["Auth:ApiAppId"];

        var client = httpClientFactory.CreateClient();
        var response = await client.PostAsync(
            $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = request.ClientId,
                ["client_secret"] = request.ClientSecret,
                ["scope"] = $"api://{apiAppId}/.default",
            }),
            ct
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            return Unauthorized(error);
        }

        var result = await response.Content.ReadFromJsonAsync<TokenResponse>(ct);
        return Ok(result);
    }
}

public record TokenRequest(string ClientId, string ClientSecret);

public record TokenResponse(
    string access_token,
    int expires_in,
    string token_type
);
