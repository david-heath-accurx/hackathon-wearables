using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HealthApi.Api;

public class ApiKeyAuthFilter(IConfiguration config) : IAuthorizationFilter
{
    private const string HeaderName = "X-Api-Key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var key)
            || key != config["Auth:AdminApiKey"])
        {
            context.Result = new UnauthorizedResult();
        }
    }
}
