using SqlSyncService.Config;

namespace SqlSyncService.Pagination;

/// <summary>
/// Implements offset-based pagination using ROW_NUMBER().
/// </summary>
public class OffsetPager
{
    /// <summary>
    /// Wraps SQL query with ROW_NUMBER pagination.
    /// </summary>
    public static string WrapQuery(QueryDefinition query, int page, int pageSize)
    {
        if (string.IsNullOrWhiteSpace(query.OrderBy))
        {
            throw new ArgumentException(
                $"Query '{query.Name}' requires OrderBy for offset pagination");
        }

        // Calculate offset (1-based page numbers)
        var offset = (page - 1) * pageSize;
        var endRow = offset + pageSize;

        // Wrap query with ROW_NUMBER
        var wrappedSql = $@"
WITH PaginatedQuery AS (
    SELECT 
        ROW_NUMBER() OVER (ORDER BY {query.OrderBy}) AS __RowNum,
        *
    FROM (
        {query.Sql.TrimEnd(';')}
    ) AS BaseQuery
)
SELECT *
FROM PaginatedQuery
WHERE __RowNum > {offset} AND __RowNum <= {endRow}
ORDER BY __RowNum;";

        return wrappedSql;
    }

    /// <summary>
    /// Validates pagination parameters.
    /// </summary>
    public static (bool Valid, string? Error) ValidateParameters(int page, int pageSize)
    {
        if (page < 1)
            return (false, "Page must be >= 1");

        if (pageSize < 1)
            return (false, "PageSize must be >= 1");

        if (pageSize > 10000)
            return (false, "PageSize cannot exceed 10000");

        return (true, null);
    }
}
