using Xunit;
using SqlSyncService.Config;
using SqlSyncService.Pagination;

namespace SqlSyncService.Tests;

public class PaginationTests
{
    [Fact]
    public void OffsetPager_WrapQuery_GeneratesCorrectSql()
    {
        // Arrange
        var query = new QueryDefinition
        {
            Name = "TestQuery",
            Sql = "SELECT * FROM Customers",
            Paginable = true,
            PaginationMode = "Offset",
            OrderBy = "cus_ID"
        };

        // Act
        var wrappedSql = OffsetPager.WrapQuery(query, page: 2, pageSize: 100);

        // Assert
        Assert.Contains("ROW_NUMBER()", wrappedSql);
        Assert.Contains("ORDER BY cus_ID", wrappedSql);
        Assert.Contains("WHERE __RowNum > 100 AND __RowNum <= 200", wrappedSql);
    }

    [Fact]
    public void OffsetPager_ValidateParameters_RejectsInvalidPage()
    {
        // Act
        var (valid, error) = OffsetPager.ValidateParameters(page: 0, pageSize: 100);

        // Assert
        Assert.False(valid);
        Assert.Contains("Page", error);
    }

    [Fact]
    public void OffsetPager_ValidateParameters_RejectsTooLargePageSize()
    {
        // Act
        var (valid, error) = OffsetPager.ValidateParameters(page: 1, pageSize: 20000);

        // Assert
        Assert.False(valid);
        Assert.Contains("PageSize", error);
    }

    [Fact]
    public void TokenPager_WrapQuery_WithoutToken_GeneratesBasicSql()
    {
        // Arrange
        var query = new QueryDefinition
        {
            Name = "TestQuery",
            Sql = "SELECT * FROM Inventory",
            Paginable = true,
            PaginationMode = "Token",
            KeyColumns = new List<string> { "stc_EntryDate", "stc_ID" }
        };

        // Act
        var wrappedSql = TokenPager.WrapQuery(query, null, pageSize: 100);

        // Assert
        Assert.Contains("SELECT TOP (@PageSize)", wrappedSql);
        Assert.Contains("ORDER BY stc_EntryDate, stc_ID", wrappedSql);
        Assert.DoesNotContain("WHERE", wrappedSql);
    }

    [Fact]
    public void TokenPager_WrapQuery_WithToken_GeneratesWhereClause()
    {
        // Arrange
        var query = new QueryDefinition
        {
            Name = "TestQuery",
            Sql = "SELECT * FROM Inventory",
            Paginable = true,
            PaginationMode = "Token",
            KeyColumns = new List<string> { "stc_EntryDate", "stc_ID" }
        };

        var lastKeyValues = new Dictionary<string, object?>
        {
            ["stc_EntryDate"] = "2024-01-01",
            ["stc_ID"] = 1000
        };

        // Act
        var wrappedSql = TokenPager.WrapQuery(query, lastKeyValues, pageSize: 100);

        // Assert
        Assert.Contains("SELECT TOP (@PageSize)", wrappedSql);
        Assert.Contains("WHERE", wrappedSql);
        Assert.Contains("@Last_stc_EntryDate", wrappedSql);
        Assert.Contains("@Last_stc_ID", wrappedSql);
        Assert.Contains("ORDER BY stc_EntryDate, stc_ID", wrappedSql);
    }

    [Fact]
    public void ContinuationToken_CreateAndValidate_RoundTrip()
    {
        // Arrange
        var keyValues = new Dictionary<string, object?>
        {
            ["EntryDate"] = "2024-01-01T00:00:00",
            ["ID"] = 12345
        };

        // Act
        var token = ContinuationToken.Create(keyValues);
        var decoded = ContinuationToken.Validate(token);

        // Assert
        Assert.NotNull(decoded);
        Assert.Equal(2, decoded.Count);
        Assert.True(decoded.ContainsKey("EntryDate"));
        Assert.True(decoded.ContainsKey("ID"));
    }

    [Fact]
    public void ContinuationToken_Validate_TamperedToken_ReturnsNull()
    {
        // Arrange
        var keyValues = new Dictionary<string, object?> { ["ID"] = 123 };
        var token = ContinuationToken.Create(keyValues);
        
        // Tamper with token
        var tamperedToken = token.Substring(0, token.Length - 4) + "XXXX";

        // Act
        var decoded = ContinuationToken.Validate(tamperedToken);

        // Assert
        Assert.Null(decoded);
    }

    [Fact]
    public void ContinuationToken_Validate_InvalidBase64_ReturnsNull()
    {
        // Act
        var decoded = ContinuationToken.Validate("not-valid-base64!!!");

        // Assert
        Assert.Null(decoded);
    }

    [Fact]
    public void TokenPager_ExtractKeyValues_ReturnsCorrectValues()
    {
        // Arrange
        var row = new Dictionary<string, object?>
        {
            ["stc_EntryDate"] = "2024-01-01",
            ["stc_ID"] = 123,
            ["stc_ProductName"] = "Widget"
        };

        var keyColumns = new List<string> { "stc_EntryDate", "stc_ID" };

        // Act
        var keyValues = TokenPager.ExtractKeyValues(row, keyColumns);

        // Assert
        Assert.Equal(2, keyValues.Count);
        Assert.Equal("2024-01-01", keyValues["stc_EntryDate"]);
        Assert.Equal(123, keyValues["stc_ID"]);
    }

    [Fact]
    public void TokenPager_ExtractKeyValues_MissingColumn_ThrowsException()
    {
        // Arrange
        var row = new Dictionary<string, object?>
        {
            ["stc_ID"] = 123
        };

        var keyColumns = new List<string> { "stc_EntryDate", "stc_ID" };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            TokenPager.ExtractKeyValues(row, keyColumns));
        
        Assert.Contains("stc_EntryDate", exception.Message);
    }
}
