using System.Net;
using SqlSyncService.Config;

namespace SqlSyncService.Security;

/// <summary>
/// Middleware that enforces IP allow-list restrictions.
/// Returns 403 Forbidden for requests from non-allowed IPs.
/// </summary>
public class IpAllowlistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IpAllowlistMiddleware> _logger;
    private readonly HashSet<IPAddress> _allowedIps;
    private readonly bool _allowLoopback;

    public IpAllowlistMiddleware(
        RequestDelegate next,
        ILogger<IpAllowlistMiddleware> logger,
        AppSettings settings)
    {
        _next = next;
        _logger = logger;
        _allowedIps = new HashSet<IPAddress>();

        // Parse allowed IPs
        foreach (var ipString in settings.Security.IpAllowList)
        {
            if (IPAddress.TryParse(ipString, out var ip))
            {
                _allowedIps.Add(ip);
            }
            else
            {
                _logger.LogWarning("Invalid IP address in allow-list: {IP}", ipString);
            }
        }

        // Check if we should allow loopback (for admin UI)
        var uri = new Uri(settings.Service.ListenUrl);
        _allowLoopback = uri.Host == "localhost" || uri.Host == "127.0.0.1" || uri.Host == "::1";

        _logger.LogInformation("IP allow-list initialized with {Count} addresses", _allowedIps.Count);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip for admin endpoints (handled by Admin project)
        if (context.Request.Path.StartsWithSegments("/admin"))
        {
            await _next(context);
            return;
        }

        var remoteIp = context.Connection.RemoteIpAddress;

        if (remoteIp == null)
        {
            _logger.LogWarning("Request with null remote IP address");
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Forbidden.IP",
                message = "Unable to determine client IP address"
            });
            return;
        }

        // Allow loopback addresses if configured
        if (_allowLoopback && (remoteIp.Equals(IPAddress.Loopback) || 
                                remoteIp.Equals(IPAddress.IPv6Loopback)))
        {
            await _next(context);
            return;
        }

        // Check if IP is in allow-list
        if (!_allowedIps.Contains(remoteIp))
        {
            _logger.LogWarning("Blocked request from non-allowed IP: {IP} to {Path}", 
                remoteIp, context.Request.Path);
            
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Forbidden.IP",
                message = "Access denied from this IP address"
            });
            return;
        }

        await _next(context);
    }
}
