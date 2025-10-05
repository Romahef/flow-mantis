namespace SqlSyncService.Config;

public class AppSettings
{
    public ServiceConfig Service { get; set; } = new();
    public SecurityConfig Security { get; set; } = new();
    public DatabaseConfig Database { get; set; } = new();
    public LoggingConfig Logging { get; set; } = new();
    public AdminConfig Admin { get; set; } = new();
}

public class ServiceConfig
{
    public string ListenUrl { get; set; } = "https://0.0.0.0:8443";
}

public class SecurityConfig
{
    public bool RequireApiKey { get; set; } = true;
    public List<string> IpAllowList { get; set; } = new();
    public bool EnableHttps { get; set; } = true;
    public CertificateConfig Certificate { get; set; } = new();
    public string ApiKeyEncrypted { get; set; } = string.Empty;
}

public class CertificateConfig
{
    public string Path { get; set; } = string.Empty;
    public string PasswordEncrypted { get; set; } = string.Empty;
}

public class DatabaseConfig
{
    public string Server { get; set; } = "127.0.0.1";
    public string Instance { get; set; } = string.Empty;
    public int Port { get; set; } = 1433;
    public string Database { get; set; } = string.Empty;
    public string UsernameEncrypted { get; set; } = string.Empty;
    public string PasswordEncrypted { get; set; } = string.Empty;
    public int CommandTimeoutSeconds { get; set; } = 60;

    public string BuildConnectionString(string username, string password)
    {
        var dataSource = string.IsNullOrWhiteSpace(Instance)
            ? $"{Server},{Port}"
            : $"{Server}\\{Instance}";

        return $"Data Source={dataSource};Initial Catalog={Database};" +
               $"User ID={username};Password={password};" +
               $"TrustServerCertificate=True;Encrypt=True;";
    }
}

public class LoggingConfig
{
    public string Level { get; set; } = "Information";
    public string Directory { get; set; } = @"C:\ProgramData\SqlSyncService\logs";
}

public class AdminConfig
{
    public string ListenUrl { get; set; } = "https://localhost:9443";
    public string PassphraseEncrypted { get; set; } = string.Empty;
}

public class QueryDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Sql { get; set; } = string.Empty;
    public bool Paginable { get; set; } = false;
    public string PaginationMode { get; set; } = "Offset"; // "Offset" or "Token"
    public string OrderBy { get; set; } = string.Empty;
    public List<string> KeyColumns { get; set; } = new();
}

public class QueriesConfig
{
    public List<QueryDefinition> Queries { get; set; } = new();
}

public class RouteMapping
{
    public string Endpoint { get; set; } = string.Empty;
    public List<QueryMapping> Queries { get; set; } = new();
}

public class QueryMapping
{
    public string QueryName { get; set; } = string.Empty;
    public string TargetArray { get; set; } = string.Empty;
}

public class MappingConfig
{
    public List<RouteMapping> Routes { get; set; } = new();
}

public class IntegrationSchema
{
    public Dictionary<string, ArraySchema> Arrays { get; set; } = new();
}

public class ArraySchema
{
    public List<FieldSchema> Fields { get; set; } = new();
}

public class FieldSchema
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Nullable { get; set; } = true;
}
