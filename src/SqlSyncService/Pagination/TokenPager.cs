using SqlSyncService.Config;
using System.Text;

namespace SqlSyncService.Pagination;

/// <summary>
/// Implements token-based (keyset) pagination for efficient cursor-style navigation.
/// </summary>
public class TokenPager
{
    /// <summary>
    /// Wraps SQL query with keyset pagination using continuation token.
    /// </summary>
    public static string WrapQuery(
        QueryDefinition query, 
        Dictionary<string, object?>? lastKeyValues, 
        int pageSize)
    {
        if (query.KeyColumns == null || query.KeyColumns.Count == 0)
        {
            throw new ArgumentException(
                $"Query '{query.Name}' requires KeyColumns for token pagination");
        }

        var sql = new StringBuilder();
        sql.AppendLine("SELECT TOP (@PageSize) *");
        sql.AppendLine("FROM (");
        sql.AppendLine($"    {query.Sql.TrimEnd(';')}");
        sql.AppendLine(") AS BaseQuery");

        // Add WHERE clause if we have a continuation token
        if (lastKeyValues != null && lastKeyValues.Count > 0)
        {
            sql.AppendLine("WHERE (");
            
            // Build composite key comparison
            // Example: WHERE (Col1, Col2) > (@LastCol1, @LastCol2)
            var keyConditions = new List<string>();
            
            for (int i = 0; i < query.KeyColumns.Count; i++)
            {
                var conditions = new List<string>();
                
                // Add equality conditions for all previous keys
                for (int j = 0; j < i; j++)
                {
                    conditions.Add($"{query.KeyColumns[j]} = @Last_{query.KeyColumns[j]}");
                }
                
                // Add greater-than condition for current key
                conditions.Add($"{query.KeyColumns[i]} > @Last_{query.KeyColumns[i]}");
                
                keyConditions.Add($"    ({string.Join(" AND ", conditions)})");
            }
            
            sql.AppendLine(string.Join("\n    OR\n", keyConditions));
            sql.AppendLine(")");
        }

        // Add ORDER BY
        var orderBy = string.Join(", ", query.KeyColumns);
        sql.AppendLine($"ORDER BY {orderBy};");

        return sql.ToString();
    }

    /// <summary>
    /// Extracts key column values from the last row for continuation token.
    /// </summary>
    public static Dictionary<string, object?> ExtractKeyValues(
        Dictionary<string, object?> lastRow,
        List<string> keyColumns)
    {
        var keyValues = new Dictionary<string, object?>();
        
        foreach (var keyColumn in keyColumns)
        {
            if (lastRow.TryGetValue(keyColumn, out var value))
            {
                keyValues[keyColumn] = value;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Key column '{keyColumn}' not found in query results");
            }
        }

        return keyValues;
    }

    /// <summary>
    /// Creates parameters dictionary from last key values for SQL query.
    /// </summary>
    public static Dictionary<string, object> CreateParameters(
        Dictionary<string, object?> lastKeyValues)
    {
        var parameters = new Dictionary<string, object>();
        
        foreach (var (key, value) in lastKeyValues)
        {
            parameters[$"Last_{key}"] = value ?? DBNull.Value;
        }

        return parameters;
    }

    /// <summary>
    /// Validates pagination parameters.
    /// </summary>
    public static (bool Valid, string? Error) ValidateParameters(int pageSize)
    {
        if (pageSize < 1)
            return (false, "PageSize must be >= 1");

        if (pageSize > 10000)
            return (false, "PageSize cannot exceed 10000");

        return (true, null);
    }
}
