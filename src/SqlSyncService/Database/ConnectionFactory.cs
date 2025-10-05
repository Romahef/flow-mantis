using Microsoft.Data.SqlClient;
using SqlSyncService.Config;

namespace SqlSyncService.Database;

/// <summary>
/// Factory for creating SQL Server connections with proper credential handling.
/// </summary>
public class ConnectionFactory
{
    private readonly DatabaseConfig _config;
    private readonly string _username;
    private readonly string _password;
    private readonly ILogger<ConnectionFactory> _logger;

    public ConnectionFactory(AppSettings settings, ILogger<ConnectionFactory> logger)
    {
        _config = settings.Database;
        _username = ConfigStore.Secrets.GetDatabaseUsername(settings);
        _password = ConfigStore.Secrets.GetDatabasePassword(settings);
        _logger = logger;
    }

    /// <summary>
    /// Creates and opens a new SQL Server connection.
    /// </summary>
    public async Task<SqlConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = _config.BuildConnectionString(_username, _password);
        var connection = new SqlConnection(connectionString);

        try
        {
            await connection.OpenAsync(cancellationToken);
            _logger.LogDebug("Opened database connection to {Server}/{Database}", 
                _config.Server, _config.Database);
            return connection;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Failed to connect to database {Server}/{Database}", 
                _config.Server, _config.Database);
            connection.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Tests database connectivity.
    /// </summary>
    public async Task<(bool Success, string Message)> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await CreateConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT @@VERSION";
            command.CommandTimeout = 5;

            var version = await command.ExecuteScalarAsync(cancellationToken);
            _logger.LogInformation("Database connection test successful");
            return (true, $"Connection successful. Server version: {version}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection test failed");
            return (false, $"Connection failed: {ex.Message}");
        }
    }

    public int CommandTimeout => _config.CommandTimeoutSeconds;
}
