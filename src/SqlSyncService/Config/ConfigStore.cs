using System.Text.Json;

namespace SqlSyncService.Config;

/// <summary>
/// Manages loading and saving configuration files with automatic secret decryption.
/// </summary>
public class ConfigStore
{
    private readonly string _configDirectory;
    private readonly ILogger<ConfigStore> _logger;

    public ConfigStore(string configDirectory, ILogger<ConfigStore> logger)
    {
        _configDirectory = configDirectory;
        _logger = logger;
    }

    public AppSettings LoadAppSettings()
    {
        var path = Path.Combine(_configDirectory, "appsettings.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Configuration file not found: {path}");
        }

        var json = File.ReadAllText(path);
        var settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        }) ?? new AppSettings();

        _logger.LogInformation("Loaded appsettings.json from {Path}", path);
        return settings;
    }

    public void SaveAppSettings(AppSettings settings)
    {
        var path = Path.Combine(_configDirectory, "appsettings.json");
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(path, json);
        _logger.LogInformation("Saved appsettings.json to {Path}", path);
    }

    public QueriesConfig LoadQueries()
    {
        var path = Path.Combine(_configDirectory, "queries.json");
        if (!File.Exists(path))
        {
            return new QueriesConfig(); // Return empty if not found
        }

        var json = File.ReadAllText(path);
        var queries = JsonSerializer.Deserialize<QueriesConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new QueriesConfig();

        _logger.LogInformation("Loaded queries.json with {Count} queries", queries.Queries.Count);
        return queries;
    }

    public void SaveQueries(QueriesConfig queries)
    {
        var path = Path.Combine(_configDirectory, "queries.json");
        var json = JsonSerializer.Serialize(queries, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(path, json);
        _logger.LogInformation("Saved queries.json with {Count} queries", queries.Queries.Count);
    }

    public MappingConfig LoadMapping()
    {
        var path = Path.Combine(_configDirectory, "mapping.json");
        if (!File.Exists(path))
        {
            return new MappingConfig(); // Return empty if not found
        }

        var json = File.ReadAllText(path);
        var mapping = JsonSerializer.Deserialize<MappingConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new MappingConfig();

        _logger.LogInformation("Loaded mapping.json with {Count} routes", mapping.Routes.Count);
        return mapping;
    }

    public void SaveMapping(MappingConfig mapping)
    {
        var path = Path.Combine(_configDirectory, "mapping.json");
        var json = JsonSerializer.Serialize(mapping, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(path, json);
        _logger.LogInformation("Saved mapping.json with {Count} routes", mapping.Routes.Count);
    }

    public IntegrationSchema LoadIntegrationSchema(string schemaPath)
    {
        if (!File.Exists(schemaPath))
        {
            throw new FileNotFoundException($"Integration schema not found: {schemaPath}");
        }

        var json = File.ReadAllText(schemaPath);
        var schema = JsonSerializer.Deserialize<IntegrationSchema>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new IntegrationSchema();

        _logger.LogInformation("Loaded integration.json with {Count} arrays", schema.Arrays.Count);
        return schema;
    }

    /// <summary>
    /// Validates that all TargetArray values in mapping exist in integration schema.
    /// </summary>
    public List<string> ValidateMapping(MappingConfig mapping, IntegrationSchema schema)
    {
        var errors = new List<string>();

        foreach (var route in mapping.Routes)
        {
            foreach (var query in route.Queries)
            {
                if (!schema.Arrays.ContainsKey(query.TargetArray))
                {
                    errors.Add($"Route '{route.Endpoint}' references undefined array '{query.TargetArray}' in integration.json");
                }
            }
        }

        return errors;
    }

    /// <summary>
    /// Decrypts secrets from configuration.
    /// </summary>
    public static class Secrets
    {
        public static string GetApiKey(AppSettings settings)
        {
            return SecretsProtector.Unprotect(settings.Security.ApiKeyEncrypted);
        }

        public static string GetCertificatePassword(AppSettings settings)
        {
            return SecretsProtector.Unprotect(settings.Security.Certificate.PasswordEncrypted);
        }

        public static string GetDatabaseUsername(AppSettings settings)
        {
            return SecretsProtector.Unprotect(settings.Database.UsernameEncrypted);
        }

        public static string GetDatabasePassword(AppSettings settings)
        {
            return SecretsProtector.Unprotect(settings.Database.PasswordEncrypted);
        }

        public static string GetAdminPassphrase(AppSettings settings)
        {
            return SecretsProtector.Unprotect(settings.Admin.PassphraseEncrypted);
        }
    }
}
