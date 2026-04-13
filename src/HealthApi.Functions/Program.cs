using HealthApi.EntityFramework;
using HealthApi.Functions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();

        services.AddDbContext<HealthApiDbContext>(options =>
            options.UseSqlServer(context.Configuration.GetConnectionString("HealthApiDb")));

        services.AddScoped<HealthDataStorage>();
        services.AddScoped<AlertStorage>();
        services.AddScoped<PatientInitiatedMessagingClient>();
        services.AddScoped<HealthMonitoringAgent>();

        services.AddHttpClient("anthropic", client =>
        {
            client.BaseAddress = new Uri("https://api.anthropic.com");
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        });

        services.AddHttpClient("patientInitiated", client =>
        {
            client.BaseAddress = new Uri(
                context.Configuration["PatientInitiatedMessaging:BaseUrl"]
                ?? "https://dev.accurx.nhs.uk");
        });

        services.AddHttpClient("patientInitiatedForms", client =>
        {
            client.BaseAddress = new Uri(
                context.Configuration["PatientInitiatedMessaging:FormsBaseUrl"]
                ?? "https://web.dev.accurx.com");
        });
    })
    .Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Health monitoring run started at {Time}", DateTimeOffset.UtcNow);

await using var scope = host.Services.CreateAsyncScope();
var agent = scope.ServiceProvider.GetRequiredService<HealthMonitoringAgent>();
await agent.RunAsync(CancellationToken.None);

logger.LogInformation("Health monitoring run completed at {Time}", DateTimeOffset.UtcNow);
