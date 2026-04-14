using System.Reflection;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using HealthApi.EntityFramework;
using HealthApi.Functions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Wearables Health API",
        Version = "v1",
        Description = "Store and retrieve personal health data from wearable devices such as Apple Watch. " +
                      "Authenticate via POST /auth/token using a registered device and patient details.",
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Obtain a token from POST /auth/token, then enter it here.",
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

builder.Services.AddDbContext<HealthApiDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HealthApiDb"))
);

builder.Services.AddScoped<HealthDataStorage>();
builder.Services.AddScoped<DeviceRegistrationStorage>();
builder.Services.AddScoped<AlertStorage>();
builder.Services.AddScoped<PatientInitiatedMessagingClient>();
builder.Services.AddScoped<ApiKeyAuthFilter>();

builder.Services.AddHttpClient("patientInitiated", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["PatientInitiatedMessaging:BaseUrl"]
        ?? "https://dev.accurx.nhs.uk");
});

builder.Services.AddHttpClient("patientInitiatedForms", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["PatientInitiatedMessaging:FormsBaseUrl"]
        ?? "https://florey.dev.accurx.com");
});

var signingKey = new SymmetricSecurityKey(
    Convert.FromBase64String(builder.Configuration["Auth:SigningKey"]!)
);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Auth:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Auth:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
        };
    });

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Wearables Health API v1");
    options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
