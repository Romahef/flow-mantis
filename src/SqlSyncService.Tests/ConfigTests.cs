using Xunit;
using SqlSyncService.Config;
using Microsoft.Extensions.Logging.Abstractions;

namespace SqlSyncService.Tests;

public class ConfigTests
{
    [Fact]
    public void DatabaseConfig_BuildConnectionString_WithoutInstance()
    {
        // Arrange
        var config = new DatabaseConfig
        {
            Server = "localhost",
            Port = 1433,
            Database = "TestDb"
        };

        // Act
        var connectionString = config.BuildConnectionString("testuser", "testpass");

        // Assert
        Assert.Contains("Data Source=localhost,1433", connectionString);
        Assert.Contains("Initial Catalog=TestDb", connectionString);
        Assert.Contains("User ID=testuser", connectionString);
        Assert.Contains("Password=testpass", connectionString);
    }

    [Fact]
    public void DatabaseConfig_BuildConnectionString_WithInstance()
    {
        // Arrange
        var config = new DatabaseConfig
        {
            Server = "localhost",
            Instance = "SQLEXPRESS",
            Database = "TestDb"
        };

        // Act
        var connectionString = config.BuildConnectionString("testuser", "testpass");

        // Assert
        Assert.Contains(@"Data Source=localhost\SQLEXPRESS", connectionString);
    }

    [Fact]
    public void ConfigStore_SaveAndLoad_RoundTrip()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var configStore = new ConfigStore(tempDir, NullLogger<ConfigStore>.Instance);

        var settings = new AppSettings
        {
            Service = new ServiceConfig { ListenUrl = "https://localhost:8443" },
            Security = new SecurityConfig
            {
                IpAllowList = new List<string> { "127.0.0.1", "192.168.1.10" }
            },
            Database = new DatabaseConfig
            {
                Server = "localhost",
                Port = 1433,
                Database = "TestDb"
            }
        };

        // Act
        configStore.SaveAppSettings(settings);
        var loaded = configStore.LoadAppSettings();

        // Assert
        Assert.Equal(settings.Service.ListenUrl, loaded.Service.ListenUrl);
        Assert.Equal(2, loaded.Security.IpAllowList.Count);
        Assert.Contains("127.0.0.1", loaded.Security.IpAllowList);
        Assert.Equal("TestDb", loaded.Database.Database);

        // Cleanup
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void ConfigStore_LoadQueries_EmptyFile_ReturnsEmpty()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var configStore = new ConfigStore(tempDir, NullLogger<ConfigStore>.Instance);

        // Act
        var queries = configStore.LoadQueries();

        // Assert
        Assert.Empty(queries.Queries);

        // Cleanup
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void ConfigStore_SaveAndLoadQueries_RoundTrip()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var configStore = new ConfigStore(tempDir, NullLogger<ConfigStore>.Instance);

        var queries = new QueriesConfig
        {
            Queries = new List<QueryDefinition>
            {
                new QueryDefinition
                {
                    Name = "TestQuery",
                    Sql = "SELECT * FROM Test",
                    Paginable = true,
                    PaginationMode = "Offset",
                    OrderBy = "ID"
                }
            }
        };

        // Act
        configStore.SaveQueries(queries);
        var loaded = configStore.LoadQueries();

        // Assert
        Assert.Single(loaded.Queries);
        Assert.Equal("TestQuery", loaded.Queries[0].Name);
        Assert.True(loaded.Queries[0].Paginable);
        Assert.Equal("Offset", loaded.Queries[0].PaginationMode);

        // Cleanup
        Directory.Delete(tempDir, true);
    }
}
