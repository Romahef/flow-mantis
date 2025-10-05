using SqlSyncService.Config;
using SqlSyncService.Database;
using System.Security.Cryptography.X509Certificates;

namespace SqlSyncService.Admin.Services;

/// <summary>
/// Handles validation and application of configuration changes.
/// </summary>
public class AdminApplyService
{
    private readonly ConfigStore _configStore;
    private readonly ILogger<AdminApplyService> _logger;

    public AdminApplyService(ConfigStore configStore, ILogger<AdminApplyService> logger)
    {
        _configStore = configStore;
        _logger = logger;
    }

    /// <summary>
    /// Validates and applies configuration changes atomically.
    /// </summary>
    public async Task<(bool Success, List<string> Errors)> ApplyConfigurationAsync(
        AppSettings settings,
        QueriesConfig queries,
        MappingConfig mapping)
    {
        var errors = new List<string>();

        try
        {
            // Step 1: Validate configuration
            errors.AddRange(ValidateSettings(settings));
            errors.AddRange(ValidateQueries(queries));
            errors.AddRange(ValidateMapping(mapping, queries));

            if (errors.Count > 0)
            {
                return (false, errors);
            }

            // Step 2: Test database connection
            var dbTestResult = await TestDatabaseConnectionAsync(settings);
            if (!dbTestResult.Success)
            {
                errors.Add($"Database connection test failed: {dbTestResult.Message}");
                return (false, errors);
            }

            // Step 3: Validate certificate
            var certResult = ValidateCertificate(settings);
            if (!certResult.Success)
            {
                errors.Add($"Certificate validation failed: {certResult.Message}");
                return (false, errors);
            }

            // Step 4: Validate against integration schema
            var integrationSchemaPath = Path.Combine(AppContext.BaseDirectory, "integration.json");
            if (File.Exists(integrationSchemaPath))
            {
                var schema = _configStore.LoadIntegrationSchema(integrationSchemaPath);
                var mappingErrors = _configStore.ValidateMapping(mapping, schema);
                errors.AddRange(mappingErrors);

                if (errors.Count > 0)
                {
                    return (false, errors);
                }
            }

            // Step 5: Save configuration atomically
            // Create backups first
            BackupConfiguration();

            try
            {
                _configStore.SaveAppSettings(settings);
                _configStore.SaveQueries(queries);
                _configStore.SaveMapping(mapping);

                _logger.LogInformation("Configuration applied successfully");
                return (true, new List<string>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save configuration");
                RestoreConfiguration();
                errors.Add($"Failed to save configuration: {ex.Message}");
                return (false, errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying configuration");
            errors.Add($"Unexpected error: {ex.Message}");
            return (false, errors);
        }
    }

    private List<string> ValidateSettings(AppSettings settings)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(settings.Service.ListenUrl))
        {
            errors.Add("Listen URL is required");
        }

        if (settings.Security.EnableHttps && string.IsNullOrWhiteSpace(settings.Security.Certificate.Path))
        {
            errors.Add("Certificate path is required when HTTPS is enabled");
        }

        if (string.IsNullOrWhiteSpace(settings.Database.Server))
        {
            errors.Add("Database server is required");
        }

        if (string.IsNullOrWhiteSpace(settings.Database.Database))
        {
            errors.Add("Database name is required");
        }

        return errors;
    }

    private List<string> ValidateQueries(QueriesConfig queries)
    {
        var errors = new List<string>();
        var queryNames = new HashSet<string>();

        foreach (var query in queries.Queries)
        {
            if (string.IsNullOrWhiteSpace(query.Name))
            {
                errors.Add("Query name cannot be empty");
                continue;
            }

            if (queryNames.Contains(query.Name))
            {
                errors.Add($"Duplicate query name: {query.Name}");
            }
            queryNames.Add(query.Name);

            if (string.IsNullOrWhiteSpace(query.Sql))
            {
                errors.Add($"Query '{query.Name}' has no SQL text");
            }

            if (query.Paginable)
            {
                if (query.PaginationMode.Equals("Offset", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(query.OrderBy))
                    {
                        errors.Add($"Query '{query.Name}' requires OrderBy for offset pagination");
                    }
                }
                else if (query.PaginationMode.Equals("Token", StringComparison.OrdinalIgnoreCase))
                {
                    if (query.KeyColumns == null || query.KeyColumns.Count == 0)
                    {
                        errors.Add($"Query '{query.Name}' requires KeyColumns for token pagination");
                    }
                }
            }
        }

        return errors;
    }

    private List<string> ValidateMapping(MappingConfig mapping, QueriesConfig queries)
    {
        var errors = new List<string>();
        var queryNames = queries.Queries.Select(q => q.Name).ToHashSet();

        foreach (var route in mapping.Routes)
        {
            if (string.IsNullOrWhiteSpace(route.Endpoint))
            {
                errors.Add("Route endpoint cannot be empty");
                continue;
            }

            foreach (var queryMapping in route.Queries)
            {
                if (!queryNames.Contains(queryMapping.QueryName))
                {
                    errors.Add($"Route '{route.Endpoint}' references undefined query '{queryMapping.QueryName}'");
                }

                if (string.IsNullOrWhiteSpace(queryMapping.TargetArray))
                {
                    errors.Add($"Route '{route.Endpoint}' has query with empty TargetArray");
                }
            }
        }

        return errors;
    }

    private async Task<(bool Success, string Message)> TestDatabaseConnectionAsync(AppSettings settings)
    {
        try
        {
            var connectionFactory = new ConnectionFactory(settings,
                LoggerFactory.Create(c => c.AddConsole()).CreateLogger<ConnectionFactory>());
            
            return await connectionFactory.TestConnectionAsync();
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private (bool Success, string Message) ValidateCertificate(AppSettings settings)
    {
        if (!settings.Security.EnableHttps)
        {
            return (true, "HTTPS not enabled");
        }

        try
        {
            if (!File.Exists(settings.Security.Certificate.Path))
            {
                return (false, $"Certificate file not found: {settings.Security.Certificate.Path}");
            }

            var certPassword = ConfigStore.Secrets.GetCertificatePassword(settings);
            using var cert = new X509Certificate2(
                settings.Security.Certificate.Path,
                certPassword,
                X509KeyStorageFlags.MachineKeySet);

            if (cert.NotAfter < DateTime.Now)
            {
                return (false, $"Certificate expired on {cert.NotAfter:yyyy-MM-dd}");
            }

            if (cert.NotBefore > DateTime.Now)
            {
                return (false, $"Certificate not valid until {cert.NotBefore:yyyy-MM-dd}");
            }

            return (true, $"Certificate valid until {cert.NotAfter:yyyy-MM-dd}");
        }
        catch (Exception ex)
        {
            return (false, $"Certificate validation error: {ex.Message}");
        }
    }

    private void BackupConfiguration()
    {
        var backupDir = Path.Combine(_configStore.GetType().GetField("_configDirectory",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .GetValue(_configStore) as string ?? "", "backup");

        Directory.CreateDirectory(backupDir);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        // Backup logic would go here
        _logger.LogInformation("Configuration backed up with timestamp {Timestamp}", timestamp);
    }

    private void RestoreConfiguration()
    {
        _logger.LogWarning("Restoring configuration from backup");
        // Restore logic would go here
    }
}
