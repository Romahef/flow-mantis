using SqlSyncService.Config;

namespace SqlSyncService.Security;

/// <summary>
/// Middleware that validates the X-API-Key header.
/// Returns 401 Unauthorized for missing or invalid API keys.
/// </summary>
public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyMiddleware> _logger;
    private readonly string _expectedApiKey;
    private readonly bool _requireApiKey;

    public ApiKeyMiddleware(
        RequestDelegate next,
        ILogger<ApiKeyMiddleware> logger,
        AppSettings settings)
    {
        _next = next;
        _logger = logger;
        _requireApiKey = settings.Security.RequireApiKey;
        _expectedApiKey = ConfigStore.Secrets.GetApiKey(settings);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip for health check and admin endpoints
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/admin"))
        {
            await _next(context);
            return;
        }

        if (!_requireApiKey)
        {
            await _next(context);
            return;
        }

        // Check for X-API-Key header
        if (!context.Request.Headers.TryGetValue("X-API-Key", out var apiKey) ||
            string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Request without API key from {IP} to {Path}",
                context.Connection.RemoteIpAddress, context.Request.Path);

            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Unauthorized.ApiKey",
                message = "Missing or invalid API key"
            });
            return;
        }

        // Validate API key (constant-time comparison to prevent timing attacks)
        if (!CryptographicEquals(apiKey.ToString(), _expectedApiKey))
        {
            _logger.LogWarning("Request with invalid API key from {IP} to {Path}",
                context.Connection.RemoteIpAddress, context.Request.Path);

            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Unauthorized.ApiKey",
                message = "Missing or invalid API key"
            });
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// Constant-time string comparison to prevent timing attacks.
    /// </summary>
    private static bool CryptographicEquals(string a, string b)
    {
        if (a.Length != b.Length)
            return false;

        int result = 0;
        for (int i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }
        return result == 0;
    }
}
