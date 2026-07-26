namespace SeeSharp.Api.Auth;

public sealed class TokenAuthMiddleware(RequestDelegate next, IConfiguration config)
{
    private readonly string _token = config["Auth:Token"]
        ?? throw new InvalidOperationException("Auth:Token is not configured.");

    private static readonly string[] OpenPrefixes = ["/swagger", "/health", "/openapi"];

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
        if (OpenPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        var header = context.Request.Headers.Authorization.ToString();
        var provided = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header["Bearer ".Length..].Trim()
            : null;

        if (provided != _token)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
            return;
        }

        await next(context);
    }
}
