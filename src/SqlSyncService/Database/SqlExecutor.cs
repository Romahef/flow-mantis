using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace SqlSyncService.Database;

/// <summary>
/// Executes SQL queries and returns results as JSON-serializable objects.
/// </summary>
public class SqlExecutor
{
    private readonly ConnectionFactory _connectionFactory;
    private readonly ILogger<SqlExecutor> _logger;

    public SqlExecutor(ConnectionFactory connectionFactory, ILogger<SqlExecutor> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <summary>
    /// Executes a query and returns results as a list of dictionaries.
    /// </summary>
    public async Task<List<Dictionary<string, object?>>> ExecuteQueryAsync(
        string sql,
        Dictionary<string, object>? parameters = null,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var results = new List<Dictionary<string, object?>>();

        try
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = timeoutSeconds ?? _connectionFactory.CommandTimeout;
            command.CommandType = CommandType.Text;

            // Add parameters if provided
            if (parameters != null)
            {
                foreach (var (key, value) in parameters)
                {
                    command.Parameters.AddWithValue($"@{key}", value ?? DBNull.Value);
                }
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            // Read all rows
            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object?>();
                
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[columnName] = NormalizeValue(value);
                }

                results.Add(row);
            }

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Query executed: {Rows} rows in {Duration}ms", 
                results.Count, duration.TotalMilliseconds);

            return results;
        }
        catch (SqlException ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "SQL execution failed after {Duration}ms", duration.TotalMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Executes a query and streams results to avoid full materialization.
    /// </summary>
    public async IAsyncEnumerable<Dictionary<string, object?>> ExecuteQueryStreamAsync(
        string sql,
        Dictionary<string, object>? parameters = null,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.CommandTimeout = timeoutSeconds ?? _connectionFactory.CommandTimeout;
        command.CommandType = CommandType.Text;

        // Add parameters if provided
        if (parameters != null)
        {
            foreach (var (key, value) in parameters)
            {
                command.Parameters.AddWithValue($"@{key}", value ?? DBNull.Value);
            }
        }

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        
        int rowCount = 0;
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>();
            
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                row[columnName] = NormalizeValue(value);
            }

            rowCount++;
            yield return row;
        }

        _logger.LogInformation("Query streamed: {Rows} rows", rowCount);
    }

    /// <summary>
    /// Normalizes database values to JSON-compatible types.
    /// </summary>
    private static object? NormalizeValue(object? value)
    {
        if (value == null || value is DBNull)
            return null;

        return value switch
        {
            DateTime dt => dt.ToString("o"), // ISO 8601 format
            DateTimeOffset dto => dto.ToString("o"),
            TimeSpan ts => ts.ToString(),
            byte[] bytes => Convert.ToBase64String(bytes),
            Guid guid => guid.ToString(),
            decimal dec => dec, // Keep as decimal for JSON serialization
            _ => value
        };
    }

    /// <summary>
    /// Executes a query and returns the first row (for preview/testing).
    /// </summary>
    public async Task<Dictionary<string, object?>?> ExecuteQueryFirstAsync(
        string sql,
        Dictionary<string, object>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var results = await ExecuteQueryAsync(sql, parameters, 5, cancellationToken);
        return results.FirstOrDefault();
    }
}
