using System.Collections.ObjectModel;
using PAMS.Application.Helpers;

namespace PAMS.UnitTests.Application;

/// <summary>
/// TDD Red Phase — verifies SortHelper.Parse behaviour.
/// These tests will NOT COMPILE because PAMS.Application.Helpers.SortHelper does not exist yet.
/// </summary>
public sealed class SortHelperTests
{
    private static readonly IReadOnlyDictionary<string, string> AllocationFields =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["fromDate"] = "FromDate",
            ["toDate"] = "ToDate",
            ["percentage"] = "Percentage",
            ["employeeName"] = "EmployeeName",
            ["projectName"] = "ProjectName",
            ["createdAt"] = "CreatedAt",
            ["status"] = "Status"
        });

    [Fact(DisplayName = "SortHelper | Parse_NullInput_ReturnsDefault")]
    public void Parse_NullInput_ReturnsDefault()
    {
        // Act
        var result = SortHelper.Parse(null, "CreatedAt", true, AllocationFields);

        // Assert
        Assert.Equal("CreatedAt", result.PropertyName);
        Assert.True(result.IsDescending);
    }

    [Fact(DisplayName = "SortHelper | Parse_EmptyInput_ReturnsDefault")]
    public void Parse_EmptyInput_ReturnsDefault()
    {
        // Act
        var result = SortHelper.Parse("", "CreatedAt", true, AllocationFields);

        // Assert
        Assert.Equal("CreatedAt", result.PropertyName);
        Assert.True(result.IsDescending);
    }

    [Fact(DisplayName = "SortHelper | Parse_ValidAscending_ReturnsFieldAndFalse")]
    public void Parse_ValidAscending_ReturnsFieldAndFalse()
    {
        // Act
        var result = SortHelper.Parse("projectName", "CreatedAt", true, AllocationFields);

        // Assert
        Assert.Equal("ProjectName", result.PropertyName);
        Assert.False(result.IsDescending);
    }

    [Fact(DisplayName = "SortHelper | Parse_ValidDescending_ReturnsFieldAndTrue")]
    public void Parse_ValidDescending_ReturnsFieldAndTrue()
    {
        // Act
        var result = SortHelper.Parse("-fromDate", "CreatedAt", false, AllocationFields);

        // Assert
        Assert.Equal("FromDate", result.PropertyName);
        Assert.True(result.IsDescending);
    }

    [Fact(DisplayName = "SortHelper | Parse_CaseInsensitive_MatchesField")]
    public void Parse_CaseInsensitive_MatchesField()
    {
        // Act
        var result = SortHelper.Parse("PROJECTNAME", "CreatedAt", true, AllocationFields);

        // Assert
        Assert.Equal("ProjectName", result.PropertyName);
        Assert.False(result.IsDescending);
    }

    [Fact(DisplayName = "SortHelper | Parse_InvalidField_ThrowsArgumentException")]
    public void Parse_InvalidField_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(
            () => SortHelper.Parse("invalidField", "CreatedAt", true, AllocationFields));

        Assert.Contains("fromDate", ex.Message);
    }

    [Fact(DisplayName = "SortHelper | Parse_DashOnly_ThrowsArgumentException")]
    public void Parse_DashOnly_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => SortHelper.Parse("-", "CreatedAt", true, AllocationFields));
    }
}
