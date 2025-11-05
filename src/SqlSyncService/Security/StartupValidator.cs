using SqlSyncService.Config;
using System.Security.Cryptography.X509Certificates;

namespace SqlSyncService.Security;

/// <summary>
/// Validates security requirements at startup and fails fast if not met.
/// </summary>
public static class StartupValidator
{
    public static void ValidateSecurityRequirements(AppSettings settings, ILogger logger)
    {
        var errors = new List<string>();

        // Parse listen URL
        if (!Uri.TryCreate(settings.Service.ListenUrl, UriKind.Absolute, out var listenUri))
        {
            errors.Add($"Invalid listen URL: {settings.Service.ListenUrl}");
        }
        else
        {
            var isLoopback = listenUri.Host == "localhost" || 
                           listenUri.Host == "127.0.0.1" || 
                           listenUri.Host == "::1";

            // Rule 1: HTTPS required (except for localhost testing)
            if (!settings.Security.EnableHttps && listenUri.Scheme != "https" && !isLoopback)
            {
                errors.Add("HTTPS is disabled but required for non-localhost addresses. Set Security.EnableHttps=true");
            }

            // Rule 2: If bound to non-loopback and HTTPS disabled → fail
            if (!isLoopback && !settings.Security.EnableHttps)
            {
                errors.Add("Cannot bind to non-loopback address without HTTPS enabled");
            }

            // Rule 3: If bound to non-loopback and allow-list empty → fail
            if (!isLoopback && settings.Security.IpAllowList.Count == 0)
            {
                errors.Add("IP allow-list cannot be empty when binding to non-loopback address");
            }
        }

        // Rule 4: Certificate required when HTTPS enabled
        if (settings.Security.EnableHttps)
        {
            if (string.IsNullOrWhiteSpace(settings.Security.Certificate.Path))
            {
                errors.Add("Certificate path is required when HTTPS is enabled");
            }
            else if (!File.Exists(settings.Security.Certificate.Path))
            {
                errors.Add($"Certificate file not found: {settings.Security.Certificate.Path}");
            }
            else
            {
                // Validate certificate can be loaded
                try
                {
                    var certPassword = ConfigStore.Secrets.GetCertificatePassword(settings);
                    using var cert = new X509Certificate2(
                        settings.Security.Certificate.Path,
                        certPassword,
                        X509KeyStorageFlags.MachineKeySet);

                    // Check certificate validity
                    if (cert.NotAfter < DateTime.Now)
                    {
                        logger.LogWarning("Certificate has expired: {NotAfter}", cert.NotAfter);
                    }
                    else if (cert.NotBefore > DateTime.Now)
                    {
                        errors.Add($"Certificate is not yet valid: {cert.NotBefore}");
                    }

                    logger.LogInformation("Certificate validated: Subject={Subject}, Expires={Expires}",
                        cert.Subject, cert.NotAfter);
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to load certificate: {ex.Message}");
                }
            }
        }

        // Validate API key is set
        if (settings.Security.RequireApiKey)
        {
            if (string.IsNullOrWhiteSpace(settings.Security.ApiKeyEncrypted))
            {
                errors.Add("API key is required when RequireApiKey is enabled");
            }
            else
            {
                try
                {
                    var apiKey = ConfigStore.Secrets.GetApiKey(settings);
                    if (string.IsNullOrWhiteSpace(apiKey))
                    {
                        errors.Add("Decrypted API key is empty");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to decrypt API key: {ex.Message}");
                }
            }
        }

        // Validate database configuration
        if (string.IsNullOrWhiteSpace(settings.Database.Database))
        {
            errors.Add("Database name is required");
        }

        if (string.IsNullOrWhiteSpace(settings.Database.UsernameEncrypted))
        {
            errors.Add("Database username is required");
        }

        if (string.IsNullOrWhiteSpace(settings.Database.PasswordEncrypted))
        {
            errors.Add("Database password is required");
        }

        // If any errors, fail startup
        if (errors.Count > 0)
        {
            logger.LogCritical("Security validation failed with {Count} errors:", errors.Count);
            foreach (var error in errors)
            {
                logger.LogCritical("  - {Error}", error);
            }
            throw new InvalidOperationException(
                $"Security validation failed. {errors.Count} error(s) found. See logs for details.");
        }

        logger.LogInformation("Security validation passed");
    }
}
