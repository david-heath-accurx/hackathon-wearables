using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace HealthApi.Functions;

public class HealthMonitorFunction(HealthMonitoringAgent agent, ILogger<HealthMonitorFunction> logger)
{
    [Function("HealthMonitor")]
    public async Task Run([TimerTrigger("0 */15 * * * *")] TimerInfo timer, CancellationToken ct)
    {
        logger.LogInformation("Health monitor triggered at {Time}", DateTimeOffset.UtcNow);
        await agent.RunAsync(ct);
    }
}
