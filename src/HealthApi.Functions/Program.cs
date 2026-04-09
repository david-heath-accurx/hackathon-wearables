using HealthApi.EntityFramework;
using HealthApi.Functions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<HealthApiDbContext>(options =>
            options.UseSqlServer(context.Configuration.GetConnectionString("HealthApiDb")));

        services.AddScoped<HealthDataStorage>();
        services.AddScoped<AlertStorage>();
        services.AddScoped<HealthMonitoringAgent>();

        services.AddHttpClient("anthropic", client =>
        {
            client.BaseAddress = new Uri("https://api.anthropic.com");
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        });
    })
    .Build();

await host.RunAsync();
