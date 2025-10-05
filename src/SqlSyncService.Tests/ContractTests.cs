using Xunit;
using SqlSyncService.Api;
using SqlSyncService.Config;
using Microsoft.Extensions.Logging.Abstractions;

namespace SqlSyncService.Tests;

public class ContractTests
{
    [Fact]
    public void ContractValidator_ValidateMapping_AllArraysExist_ReturnsNoErrors()
    {
        // Arrange
        var schema = new IntegrationSchema
        {
            Arrays = new Dictionary<string, ArraySchema>
            {
                ["warehouses"] = new ArraySchema(),
                ["customers"] = new ArraySchema()
            }
        };

        var mapping = new MappingConfig
        {
            Routes = new List<RouteMapping>
            {
                new RouteMapping
                {
                    Endpoint = "Warehouses",
                    Queries = new List<QueryMapping>
                    {
                        new QueryMapping { QueryName = "Warehouses", TargetArray = "warehouses" }
                    }
                }
            }
        };

        var validator = new ContractValidator(schema, NullLogger<ContractValidator>.Instance);

        // Act
        var errors = validator.ValidateMapping(mapping);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void ContractValidator_ValidateMapping_MissingArray_ReturnsError()
    {
        // Arrange
        var schema = new IntegrationSchema
        {
            Arrays = new Dictionary<string, ArraySchema>
            {
                ["warehouses"] = new ArraySchema()
            }
        };

        var mapping = new MappingConfig
        {
            Routes = new List<RouteMapping>
            {
                new RouteMapping
                {
                    Endpoint = "Customers",
                    Queries = new List<QueryMapping>
                    {
                        new QueryMapping { QueryName = "Customers", TargetArray = "customers" }
                    }
                }
            }
        };

        var validator = new ContractValidator(schema, NullLogger<ContractValidator>.Instance);

        // Act
        var errors = validator.ValidateMapping(mapping);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains("customers", errors[0]);
    }

    [Fact]
    public void ContractValidator_ValidateRow_ValidRow_ReturnsNoErrors()
    {
        // Arrange
        var schema = new IntegrationSchema
        {
            Arrays = new Dictionary<string, ArraySchema>
            {
                ["warehouses"] = new ArraySchema
                {
                    Fields = new List<FieldSchema>
                    {
                        new FieldSchema { Name = "whs_ID", Type = "int", Nullable = false },
                        new FieldSchema { Name = "whs_Code", Type = "string", Nullable = false },
                        new FieldSchema { Name = "whs_Name", Type = "string", Nullable = false }
                    }
                }
            }
        };

        var row = new Dictionary<string, object?>
        {
            ["whs_ID"] = 1,
            ["whs_Code"] = "WH001",
            ["whs_Name"] = "Main Warehouse"
        };

        var validator = new ContractValidator(schema, NullLogger<ContractValidator>.Instance);

        // Act
        var errors = validator.ValidateRow("warehouses", row);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void ContractValidator_ValidateRow_MissingRequiredField_ReturnsError()
    {
        // Arrange
        var schema = new IntegrationSchema
        {
            Arrays = new Dictionary<string, ArraySchema>
            {
                ["warehouses"] = new ArraySchema
                {
                    Fields = new List<FieldSchema>
                    {
                        new FieldSchema { Name = "whs_ID", Type = "int", Nullable = false },
                        new FieldSchema { Name = "whs_Code", Type = "string", Nullable = false }
                    }
                }
            }
        };

        var row = new Dictionary<string, object?>
        {
            ["whs_ID"] = 1
            // whs_Code is missing!
        };

        var validator = new ContractValidator(schema, NullLogger<ContractValidator>.Instance);

        // Act
        var errors = validator.ValidateRow("warehouses", row);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains("whs_Code", errors[0]);
    }

    [Fact]
    public void ContractValidator_ValidateRow_ExtraField_ReturnsError()
    {
        // Arrange
        var schema = new IntegrationSchema
        {
            Arrays = new Dictionary<string, ArraySchema>
            {
                ["warehouses"] = new ArraySchema
                {
                    Fields = new List<FieldSchema>
                    {
                        new FieldSchema { Name = "whs_ID", Type = "int", Nullable = false }
                    }
                }
            }
        };

        var row = new Dictionary<string, object?>
        {
            ["whs_ID"] = 1,
            ["extra_field"] = "unexpected"
        };

        var validator = new ContractValidator(schema, NullLogger<ContractValidator>.Instance);

        // Act
        var errors = validator.ValidateRow("warehouses", row);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains("extra_field", errors[0]);
    }

    [Fact]
    public void ContractValidator_ValidateRow_NullInNonNullableField_ReturnsError()
    {
        // Arrange
        var schema = new IntegrationSchema
        {
            Arrays = new Dictionary<string, ArraySchema>
            {
                ["warehouses"] = new ArraySchema
                {
                    Fields = new List<FieldSchema>
                    {
                        new FieldSchema { Name = "whs_ID", Type = "int", Nullable = false },
                        new FieldSchema { Name = "whs_Code", Type = "string", Nullable = false }
                    }
                }
            }
        };

        var row = new Dictionary<string, object?>
        {
            ["whs_ID"] = 1,
            ["whs_Code"] = null  // Null in non-nullable field!
        };

        var validator = new ContractValidator(schema, NullLogger<ContractValidator>.Instance);

        // Act
        var errors = validator.ValidateRow("warehouses", row);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains("cannot be null", errors[0]);
    }

    [Fact]
    public void ConfigStore_ValidateMapping_ConsistentWithSchema()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var configStore = new ConfigStore(tempDir, NullLogger<ConfigStore>.Instance);

        var mapping = new MappingConfig
        {
            Routes = new List<RouteMapping>
            {
                new RouteMapping
                {
                    Endpoint = "Test",
                    Queries = new List<QueryMapping>
                    {
                        new QueryMapping { QueryName = "TestQuery", TargetArray = "nonexistent" }
                    }
                }
            }
        };

        var schema = new IntegrationSchema
        {
            Arrays = new Dictionary<string, ArraySchema>
            {
                ["warehouses"] = new ArraySchema()
            }
        };

        // Act
        var errors = configStore.ValidateMapping(mapping, schema);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains("nonexistent", errors[0]);

        // Cleanup
        Directory.Delete(tempDir, true);
    }
}
