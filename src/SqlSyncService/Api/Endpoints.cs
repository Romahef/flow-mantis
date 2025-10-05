using Microsoft.AspNetCore.Mvc;
using SqlSyncService.Config;
using SqlSyncService.Database;
using SqlSyncService.Pagination;
using SqlSyncService.Serialization;
using System.Text.Json;

namespace SqlSyncService.Api;

/// <summary>
/// Defines all API endpoints for the SQL Sync Service.
/// </summary>
public static class Endpoints
{
    public static void MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        // Health check endpoint (no auth required)
        app.MapGet("/health", GetHealth);

        // Query list endpoint
        app.MapGet("/api/queries", GetQueryList);

        // Execute queries by endpoint name
        app.MapGet("/api/queries/{endpointName}", ExecuteEndpoint);
        app.MapPost("/api/queries/{endpointName}/execute", ExecuteEndpoint);
    }

    private static IResult GetHealth()
    {
        return Results.Json(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "SqlSyncService"
        });
    }

    private static IResult GetQueryList(
        [FromServices] ConfigStore configStore)
    {
        var queries = configStore.LoadQueries();
        var mapping = configStore.LoadMapping();

        return Results.Json(new
        {
            queries = queries.Queries.Select(q => new
            {
                q.Name,
                q.Paginable,
                q.PaginationMode
            }),
            routes = mapping.Routes.Select(r => new
            {
                r.Endpoint,
                Queries = r.Queries.Select(q => new
                {
                    q.QueryName,
                    q.TargetArray
                })
            })
        });
    }

    private static async Task<IResult> ExecuteEndpoint(
        string endpointName,
        [FromServices] ConfigStore configStore,
        [FromServices] SqlExecutor sqlExecutor,
        [FromServices] ILogger<Program> logger,
        HttpContext context,
        int? timeout = null,
        int? page = null,
        int? pageSize = null,
        string? continuationToken = null,
        int? maxRows = null)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Load configuration
            var queries = configStore.LoadQueries();
            var mapping = configStore.LoadMapping();
            
            // Find route
            var route = mapping.Routes.FirstOrDefault(r => 
                r.Endpoint.Equals(endpointName, StringComparison.OrdinalIgnoreCase));

            if (route == null)
            {
                return Results.NotFound(new
                {
                    error = "NotFound.Endpoint",
                    message = $"Endpoint '{endpointName}' not found"
                });
            }

            // Build response with streaming
            var namedArrays = new Dictionary<string, IAsyncEnumerable<Dictionary<string, object?>>>();
            var paginationInfo = new Dictionary<string, object>();
            int totalRows = 0;

            foreach (var queryMapping in route.Queries)
            {
                var queryDef = queries.Queries.FirstOrDefault(q => 
                    q.Name.Equals(queryMapping.QueryName, StringComparison.OrdinalIgnoreCase));

                if (queryDef == null)
                {
                    logger.LogWarning("Query '{QueryName}' not found for endpoint '{Endpoint}'", 
                        queryMapping.QueryName, endpointName);
                    continue;
                }

                // Handle pagination
                var (sql, parameters, pagInfo) = await PreparePaginatedQuery(
                    queryDef, page, pageSize, continuationToken, logger);

                if (pagInfo != null)
                {
                    paginationInfo = pagInfo;
                }

                // Stream query results
                var resultStream = ExecuteQueryWithPaginationAsync(
                    sqlExecutor, sql, parameters, timeout, maxRows, queryDef, 
                    pageSize, paginationInfo, logger, context.RequestAborted);

                namedArrays[queryMapping.TargetArray] = resultStream;
            }

            // Set response headers
            context.Response.ContentType = "application/json";
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

            // Write streaming JSON response
            await JsonStreamWriter.WriteResponseAsync(
                context.Response.Body,
                namedArrays,
                paginationInfo.Count > 0 ? new Dictionary<string, object> { ["_page"] = paginationInfo } : null,
                context.RequestAborted);

            var duration = DateTime.UtcNow - startTime;
            logger.LogInformation(
                "Endpoint '{Endpoint}' executed successfully in {Duration}ms from {IP}",
                endpointName, duration.TotalMilliseconds, context.Connection.RemoteIpAddress);

            return Results.Empty;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Request cancelled for endpoint '{Endpoint}'", endpointName);
            return Results.StatusCode(499); // Client closed request
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing endpoint '{Endpoint}'", endpointName);
            return Results.Json(new
            {
                error = "Server.Error",
                message = "An error occurred while executing the query",
                details = ex.Message
            }, statusCode: 500);
        }
    }

    private static async Task<(string Sql, Dictionary<string, object>? Parameters, Dictionary<string, object>? PaginationInfo)> 
        PreparePaginatedQuery(
            QueryDefinition queryDef,
            int? page,
            int? pageSize,
            string? continuationToken,
            ILogger logger)
    {
        var sql = queryDef.Sql;
        Dictionary<string, object>? parameters = null;
        Dictionary<string, object>? paginationInfo = null;

        // Check if pagination requested
        bool paginationRequested = page.HasValue || pageSize.HasValue || !string.IsNullOrEmpty(continuationToken);

        if (paginationRequested && !queryDef.Paginable)
        {
            throw new InvalidOperationException(
                $"Query '{queryDef.Name}' does not support pagination");
        }

        if (!paginationRequested || !queryDef.Paginable)
        {
            return (sql, null, null);
        }

        // Handle pagination based on mode
        if (queryDef.PaginationMode.Equals("Offset", StringComparison.OrdinalIgnoreCase))
        {
            var currentPage = page ?? 1;
            var currentPageSize = pageSize ?? 100;

            var (valid, error) = OffsetPager.ValidateParameters(currentPage, currentPageSize);
            if (!valid)
            {
                throw new ArgumentException(error);
            }

            sql = OffsetPager.WrapQuery(queryDef, currentPage, currentPageSize);
            
            paginationInfo = new Dictionary<string, object>
            {
                ["mode"] = "offset",
                ["page"] = currentPage,
                ["pageSize"] = currentPageSize
            };
        }
        else if (queryDef.PaginationMode.Equals("Token", StringComparison.OrdinalIgnoreCase))
        {
            var currentPageSize = pageSize ?? 100;

            var (valid, error) = TokenPager.ValidateParameters(currentPageSize);
            if (!valid)
            {
                throw new ArgumentException(error);
            }

            Dictionary<string, object?>? lastKeyValues = null;

            if (!string.IsNullOrEmpty(continuationToken))
            {
                lastKeyValues = ContinuationToken.Validate(continuationToken);
                if (lastKeyValues == null)
                {
                    throw new ArgumentException("Invalid or tampered continuation token");
                }
            }

            sql = TokenPager.WrapQuery(queryDef, lastKeyValues, currentPageSize);
            parameters = new Dictionary<string, object> { ["PageSize"] = currentPageSize };

            if (lastKeyValues != null)
            {
                var tokenParams = TokenPager.CreateParameters(lastKeyValues);
                foreach (var (key, value) in tokenParams)
                {
                    parameters[key] = value;
                }
            }

            paginationInfo = new Dictionary<string, object>
            {
                ["mode"] = "token",
                ["pageSize"] = currentPageSize
            };
        }

        return (sql, parameters, paginationInfo);
    }

    private static async IAsyncEnumerable<Dictionary<string, object?>> ExecuteQueryWithPaginationAsync(
        SqlExecutor sqlExecutor,
        string sql,
        Dictionary<string, object>? parameters,
        int? timeout,
        int? maxRows,
        QueryDefinition queryDef,
        int? pageSize,
        Dictionary<string, object> paginationInfo,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var rowCount = 0;
        Dictionary<string, object?>? lastRow = null;

        await foreach (var row in sqlExecutor.ExecuteQueryStreamAsync(
            sql, parameters, timeout, cancellationToken))
        {
            rowCount++;
            lastRow = row;
            
            yield return row;

            // Check max rows limit
            if (maxRows.HasValue && rowCount >= maxRows.Value)
            {
                logger.LogInformation("Reached maxRows limit of {MaxRows}", maxRows.Value);
                break;
            }
        }

        // Generate continuation token for token-based pagination
        if (queryDef.PaginationMode.Equals("Token", StringComparison.OrdinalIgnoreCase) &&
            lastRow != null &&
            rowCount == (pageSize ?? 100))
        {
            try
            {
                var keyValues = TokenPager.ExtractKeyValues(lastRow, queryDef.KeyColumns);
                var token = ContinuationToken.Create(keyValues);
                paginationInfo["continuationToken"] = token;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to create continuation token");
                paginationInfo["continuationToken"] = null;
            }
        }
        else
        {
            paginationInfo["continuationToken"] = null;
        }
    }
}
