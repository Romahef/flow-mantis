using System.Text.Json;
using SqlSyncService.Config;
using SqlSyncService.Database;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JsonExampleGenerator;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=================================================");
        Console.WriteLine("  SqlSyncService - JSON Example Generator");
        Console.WriteLine("=================================================\n");

        // Parse command line arguments
        var connectionString = GetArgument(args, "--connection", "");
        var queriesPath = GetArgument(args, "--queries", "../../config-samples/queries.json");
        var outputDir = GetArgument(args, "--output", "./examples");
        var maxRows = int.Parse(GetArgument(args, "--max-rows", "0")); // 0 = unlimited

        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run -- --connection \"<connection-string>\" [options]\n");
            Console.WriteLine("Options:");
            Console.WriteLine("  --connection    SQL Server connection string (required)");
            Console.WriteLine("  --queries       Path to queries.json (default: ../../config-samples/queries.json)");
            Console.WriteLine("  --output        Output directory for JSON files (default: ./examples)");
            Console.WriteLine("  --max-rows      Maximum rows per query (default: 0 = unlimited, use 10 for sample)\n");
            Console.WriteLine("Examples:");
            Console.WriteLine("  # Connect to SQL Server");
            Console.WriteLine("  dotnet run -- --connection \"Server=localhost;Database=WMS;User Id=sa;Password=YourPass;TrustServerCertificate=True\"\n");
            Console.WriteLine("  # Connect to Docker SQL Server on Mac");
            Console.WriteLine("  dotnet run -- --connection \"Server=localhost,1433;Database=WMS;User Id=sa;Password=YourStrong@Pass;TrustServerCertificate=True\"\n");
            Console.WriteLine("  # Connect to remote SQL Server");
            Console.WriteLine("  dotnet run -- --connection \"Server=your-server.com;Database=WMS;User Id=user;Password=pass;TrustServerCertificate=True\"\n");
            return;
        }

        try
        {
            // Load queries
            Console.WriteLine($"📄 Loading queries from: {queriesPath}");
            var queries = LoadQueries(queriesPath);
            Console.WriteLine($"✓ Loaded {queries.Queries.Count} queries\n");

            // Create output directory
            Directory.CreateDirectory(outputDir);
            Console.WriteLine($"📁 Output directory: {outputDir}\n");

            // Test connection
            Console.WriteLine("🔌 Testing database connection...");
            var testSuccess = await TestConnection(connectionString);
            if (!testSuccess)
            {
                Console.WriteLine("❌ Connection test failed. Please check your connection string.\n");
                return;
            }
            Console.WriteLine("✓ Connection successful!\n");

            // Generate examples for each query
            Console.WriteLine("🚀 Generating JSON examples...\n");
            
            foreach (var query in queries.Queries)
            {
                Console.WriteLine($"───────────────────────────────────────");
                Console.WriteLine($"📝 Query: {query.Name}");
                Console.WriteLine($"   Paginable: {query.Paginable}");
                
                if (query.Paginable)
                {
                    Console.WriteLine($"   Mode: {query.PaginationMode}");
                    if (query.PaginationMode == "Offset")
                        Console.WriteLine($"   OrderBy: {query.OrderBy}");
                    else
                        Console.WriteLine($"   KeyColumns: {string.Join(", ", query.KeyColumns)}");
                }

                try
                {
                    var results = await ExecuteQuery(connectionString, query.Sql, maxRows);
                    
                    if (results.Count == 0)
                    {
                        Console.WriteLine($"⚠️  No data returned (query may need sample data)\n");
                        
                        // Create empty example
                        var emptyOutput = new { data = new List<object>(), rowCount = 0, note = "No data in database" };
                        var emptyJson = JsonSerializer.Serialize(emptyOutput, new JsonSerializerOptions { WriteIndented = true });
                        var emptyPath = Path.Combine(outputDir, $"{query.Name}_empty.json");
                        await File.WriteAllTextAsync(emptyPath, emptyJson);
                        Console.WriteLine($"   📄 Saved: {emptyPath}");
                    }
                    else
                    {
                        Console.WriteLine($"✓  Retrieved {results.Count} rows");
                        
                        // Save full results
                        var output = new 
                        { 
                            queryName = query.Name,
                            rowCount = results.Count,
                            maxRowsLimit = maxRows == 0 ? "unlimited" : maxRows.ToString(),
                            data = results
                        };
                        var json = JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
                        var path = Path.Combine(outputDir, $"{query.Name}.json");
                        await File.WriteAllTextAsync(path, json);
                        Console.WriteLine($"   📄 Saved: {path}");
                        
                        // Show sample data
                        if (results.Count > 0)
                        {
                            Console.WriteLine($"   Sample row:");
                            var sample = JsonSerializer.Serialize(results[0], new JsonSerializerOptions { WriteIndented = true });
                            foreach (var line in sample.Split('\n').Take(5))
                            {
                                Console.WriteLine($"   {line}");
                            }
                            if (sample.Split('\n').Length > 5)
                                Console.WriteLine($"   ...");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error: {ex.Message}");
                    
                    // Save error info
                    var errorOutput = new { error = ex.Message, query = query.Sql };
                    var errorJson = JsonSerializer.Serialize(errorOutput, new JsonSerializerOptions { WriteIndented = true });
                    var errorPath = Path.Combine(outputDir, $"{query.Name}_error.json");
                    await File.WriteAllTextAsync(errorPath, errorJson);
                }
                
                Console.WriteLine();
            }

            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine("✅ JSON generation complete!");
            Console.WriteLine($"📁 Output: {Path.GetFullPath(outputDir)}");
            Console.WriteLine("═══════════════════════════════════════\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Fatal error: {ex.Message}");
            Console.WriteLine($"   {ex.GetType().Name}");
            if (ex.InnerException != null)
                Console.WriteLine($"   Inner: {ex.InnerException.Message}");
        }
    }

    static string GetArgument(string[] args, string name, string defaultValue)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }
        return defaultValue;
    }

    static QueriesConfig LoadQueries(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<QueriesConfig>(json) ?? new QueriesConfig();
    }

    static async Task<bool> TestConnection(string connectionString)
    {
        try
        {
            var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync();
            
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT @@VERSION";
            var version = await command.ExecuteScalarAsync();
            
            Console.WriteLine($"   Server: {version?.ToString()?.Split('\n')[0]}");
            
            await connection.CloseAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    static async Task<List<Dictionary<string, object?>>> ExecuteQuery(
        string connectionString, 
        string sql, 
        int maxRows)
    {
        var results = new List<Dictionary<string, object?>>();
        
        var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await connection.OpenAsync();
        
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 30;
        
        using var reader = await command.ExecuteReaderAsync();
        
        int rowCount = 0;
        while (await reader.ReadAsync() && (maxRows == 0 || rowCount < maxRows))
        {
            var row = new Dictionary<string, object?>();
            
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                row[columnName] = NormalizeValue(value);
            }
            
            results.Add(row);
            rowCount++;
        }
        
        await connection.CloseAsync();
        
        return results;
    }

    static object? NormalizeValue(object? value)
    {
        if (value == null) return null;

        return value switch
        {
            DateTime dt => dt.ToString("o"),
            DateTimeOffset dto => dto.ToString("o"),
            TimeSpan ts => ts.ToString(),
            byte[] bytes => Convert.ToBase64String(bytes),
            Guid guid => guid.ToString(),
            _ => value
        };
    }
}

