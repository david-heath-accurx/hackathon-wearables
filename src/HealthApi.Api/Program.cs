using HealthApi.EntityFramework;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<HealthApiDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HealthApiDb"))
);

builder.Services.AddScoped<HealthDataStorage>();
builder.Services.AddScoped<DeviceRegistrationStorage>();
builder.Services.AddHttpClient();

var tenantId = builder.Configuration["Auth:TenantId"]!;
var apiAppId = builder.Configuration["Auth:ApiAppId"]!;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudiences = [$"api://{apiAppId}", apiAppId],
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
