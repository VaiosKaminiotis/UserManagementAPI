using System.Security.Cryptography;
using System.Text;

namespace UserManagementAPI.Middleware;

public class TokenAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenAuthenticationMiddleware> _logger;

    public TokenAuthenticationMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<TokenAuthenticationMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var expectedToken = _configuration["Authentication:Token"];

        if (string.IsNullOrWhiteSpace(expectedToken))
        {
            throw new InvalidOperationException(
                "Authentication token is not configured. Set Authentication:Token in configuration.");
        }

        if (!context.Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
        {
            await WriteUnauthorizedResponse(context);
            return;
        }

        const string bearerPrefix = "Bearer ";
        var headerValue = authorizationHeader.ToString();

        if (!headerValue.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            await WriteUnauthorizedResponse(context);
            return;
        }

        var providedToken = headerValue[bearerPrefix.Length..].Trim();

        if (!TokensMatch(providedToken, expectedToken))
        {
            _logger.LogWarning(
                "Unauthorized request to {Method} {Path}: invalid bearer token.",
                context.Request.Method,
                context.Request.Path);

            await WriteUnauthorizedResponse(context);
            return;
        }

        await _next(context);
    }

    private static bool TokensMatch(string providedToken, string expectedToken)
    {
        var providedBytes = Encoding.UTF8.GetBytes(providedToken);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedToken);

        return providedBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }

    private static async Task WriteUnauthorizedResponse(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new
        {
            error = "Unauthorized. A valid bearer token is required."
        });
    }
}
