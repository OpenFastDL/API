namespace OpenFastDL.Api;

public class ApiKeyEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        
        const string headerName = "X-Api-Key";

        if (!context.HttpContext.Request.Headers.TryGetValue(headerName, out var values) ||
            values.First() != configuration["ApiKey"])
        {
            return Results.Unauthorized();
        }

        return await next(context);
    }
}