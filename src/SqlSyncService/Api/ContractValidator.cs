using SqlSyncService.Config;

namespace SqlSyncService.Api;

/// <summary>
/// Validates that query results match the integration.json schema.
/// </summary>
public class ContractValidator
{
    private readonly IntegrationSchema _schema;
    private readonly ILogger<ContractValidator> _logger;

    public ContractValidator(IntegrationSchema schema, ILogger<ContractValidator> logger)
    {
        _schema = schema;
        _logger = logger;
    }

    /// <summary>
    /// Validates that all target arrays exist in the schema.
    /// </summary>
    public List<string> ValidateMapping(MappingConfig mapping)
    {
        var errors = new List<string>();

        foreach (var route in mapping.Routes)
        {
            foreach (var query in route.Queries)
            {
                if (!_schema.Arrays.ContainsKey(query.TargetArray))
                {
                    errors.Add(
                        $"Route '{route.Endpoint}' maps to undefined array '{query.TargetArray}' " +
                        $"in integration.json");
                }
            }
        }

        if (errors.Count > 0)
        {
            _logger.LogError("Mapping validation failed with {Count} errors", errors.Count);
        }

        return errors;
    }

    /// <summary>
    /// Validates a single row against the schema for a target array.
    /// Returns list of validation errors (empty if valid).
    /// </summary>
    public List<string> ValidateRow(string targetArray, Dictionary<string, object?> row)
    {
        var errors = new List<string>();

        if (!_schema.Arrays.TryGetValue(targetArray, out var arraySchema))
        {
            errors.Add($"Array '{targetArray}' not defined in schema");
            return errors;
        }

        // Build field map for quick lookup
        var schemaFields = arraySchema.Fields.ToDictionary(f => f.Name, f => f);

        // Check for extra fields in row
        foreach (var fieldName in row.Keys)
        {
            if (!schemaFields.ContainsKey(fieldName))
            {
                errors.Add($"Field '{fieldName}' not defined in schema for array '{targetArray}'");
            }
        }

        // Check for missing required fields and type mismatches
        foreach (var field in arraySchema.Fields)
        {
            if (!row.TryGetValue(field.Name, out var value))
            {
                if (!field.Nullable)
                {
                    errors.Add($"Required field '{field.Name}' missing in array '{targetArray}'");
                }
                continue;
            }

            // Check nullability
            if (value == null && !field.Nullable)
            {
                errors.Add($"Field '{field.Name}' in array '{targetArray}' cannot be null");
            }

            // Type validation (basic)
            if (value != null && !IsCompatibleType(value, field.Type))
            {
                errors.Add(
                    $"Field '{field.Name}' in array '{targetArray}' has incompatible type. " +
                    $"Expected: {field.Type}, Got: {value.GetType().Name}");
            }
        }

        return errors;
    }

    private static bool IsCompatibleType(object value, string expectedType)
    {
        return expectedType.ToLowerInvariant() switch
        {
            "string" => value is string,
            "int" or "integer" => value is int or long,
            "decimal" or "number" => value is decimal or double or float or int or long,
            "bool" or "boolean" => value is bool,
            "datetime" or "date" => value is DateTime or string, // Allow string for ISO dates
            "guid" => value is Guid or string,
            _ => true // Unknown types pass validation
        };
    }
}
