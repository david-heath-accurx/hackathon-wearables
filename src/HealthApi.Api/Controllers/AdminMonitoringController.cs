using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthApi.Api.Controllers;

/// <summary>Admin monitoring controls — service-to-service, authenticated via Key Vault signing key</summary>
[ApiController]
[Route("admin/monitoring")]
[Authorize(Policy = "ServiceKey")]
public class AdminMonitoringController(IConfiguration config, HttpClient httpClient) : ControllerBase
{
    /// <summary>Trigger the health monitoring job immediately</summary>
    /// <remarks>
    /// Starts a new execution of the health monitoring Container Apps Job without waiting for its next
    /// scheduled run. Useful for testing alert detection immediately after submitting data.
    /// </remarks>
    /// <response code="200">Job execution started</response>
    /// <response code="401">Missing or invalid service token</response>
    [HttpPost("trigger")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Trigger(CancellationToken ct)
    {
        var subscriptionId = config["Monitoring:SubscriptionId"];
        var resourceGroup = config["Monitoring:ResourceGroup"];
        var jobName = config["Monitoring:JobName"];

        var credential = new DefaultAzureCredential();
        var token = await credential.GetTokenAsync(
            new TokenRequestContext(["https://management.azure.com/.default"]), ct);

        var url = $"https://management.azure.com/subscriptions/{subscriptionId}" +
                  $"/resourceGroups/{resourceGroup}" +
                  $"/providers/Microsoft.App/jobs/{jobName}" +
                  $"/start?api-version=2023-05-01";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new("Bearer", token.Token);
        request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        return Ok();
    }
}
