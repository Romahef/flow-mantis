using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using SqlSyncService.Api;
using SqlSyncService.Config;
using SqlSyncService.Database;
using SqlSyncService.Security;
using System.Security.Cryptography.X509Certificates;

namespace SqlSyncService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure Windows Service hosting
        builder.Host.UseWindowsService(options =>
        {
            options.ServiceName = "SqlSyncService";
        });

        // Configure logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddEventLog(settings =>
        {
            settings.SourceName = "SqlSyncService";
        });

        // Load configuration
        var configDirectory = GetConfigDirectory();
        var logger = LoggerFactory.Create(config => config.AddConsole())
            .CreateLogger<Program>();

        logger.LogInformation("Loading configuration from {ConfigDirectory}", configDirectory);

        var configStore = new ConfigStore(configDirectory, 
            LoggerFactory.Create(c => c.AddConsole()).CreateLogger<ConfigStore>());
        
        var appSettings = configStore.LoadAppSettings();
        var queries = configStore.LoadQueries();
        var mapping = configStore.LoadMapping();

        // Validate security requirements at startup
        StartupValidator.ValidateSecurityRequirements(appSettings, logger);

        // Load and validate integration schema
        var integrationSchemaPath = Path.Combine(AppContext.BaseDirectory, "integration.json");
        var integrationSchema = configStore.LoadIntegrationSchema(integrationSchemaPath);
        
        var mappingErrors = configStore.ValidateMapping(mapping, integrationSchema);
        if (mappingErrors.Count > 0)
        {
            logger.LogCritical("Mapping validation failed:");
            foreach (var error in mappingErrors)
            {
                logger.LogCritical("  - {Error}", error);
            }
            throw new InvalidOperationException("Mapping validation failed. See logs for details.");
        }

        // Register services
        builder.Services.AddSingleton(appSettings);
        builder.Services.AddSingleton(configStore);
        builder.Services.AddSingleton(integrationSchema);
        builder.Services.AddSingleton<ConnectionFactory>();
        builder.Services.AddSingleton<SqlExecutor>();
        builder.Services.AddSingleton<ContractValidator>();

        // Configure Kestrel
        builder.WebHost.ConfigureKestrel(options =>
        {
            var uri = new Uri(appSettings.Service.ListenUrl);
            
            // Parse host to IP address (handle "localhost" hostname)
            var ipAddress = uri.Host == "localhost" ? System.Net.IPAddress.Loopback :
                           uri.Host == "0.0.0.0" ? System.Net.IPAddress.Any :
                           System.Net.IPAddress.Parse(uri.Host);
            
            options.Listen(ipAddress, uri.Port, listenOptions =>
            {
                if (appSettings.Security.EnableHttps)
                {
                    var certPassword = ConfigStore.Secrets.GetCertificatePassword(appSettings);
                    var certificate = new X509Certificate2(
                        appSettings.Security.Certificate.Path,
                        certPassword,
                        X509KeyStorageFlags.MachineKeySet);

                    listenOptions.UseHttps(certificate, httpsOptions =>
                    {
                        httpsOptions.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | 
                                                   System.Security.Authentication.SslProtocols.Tls13;
                    });
                }

                listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
            });

            options.AddServerHeader = false;
        });

        var app = builder.Build();

        // Add security middleware
        app.Use(async (context, next) =>
        {
            // Add security headers
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Append("Referrer-Policy", "no-referrer");
            await next();
        });

        // Add custom middleware
        app.UseMiddleware<IpAllowlistMiddleware>();
        app.UseMiddleware<ApiKeyMiddleware>();

        // Map API endpoints
        app.MapApiEndpoints();

        logger.LogInformation("SqlSyncService starting on {ListenUrl}", appSettings.Service.ListenUrl);

        app.Run();
    }

    private static string GetConfigDirectory()
    {
        // Check for custom config directory in environment variable
        var customPath = Environment.GetEnvironmentVariable("SQLSYNC_CONFIG_DIR");
        if (!string.IsNullOrEmpty(customPath) && Directory.Exists(customPath))
        {
            return customPath;
        }

        // Default to ProgramData
        var defaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "SqlSyncService");

        if (!Directory.Exists(defaultPath))
        {
            Directory.CreateDirectory(defaultPath);
        }

        return defaultPath;
    }
}
