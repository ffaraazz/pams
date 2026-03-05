using System.Reflection;

namespace PAMS.UnitTests.Application;

/// <summary>
/// TDD Red Phase — verifies that repository interfaces expose the updated
/// GetFilteredAsync (with sort param) and the new GetFilteredAllAsync methods
/// required for sort + export features.
/// These tests will FAIL because the interfaces have not been updated yet.
/// </summary>
public sealed class SortEndpointTests
{
    // ── GetFilteredAsync with sort parameter ─────────────────────────────

    [Fact(DisplayName = "Sort | IAllocationRepository_GetFilteredAsync_ShouldHaveSortParam")]
    public void IAllocationRepository_GetFilteredAsync_ShouldHaveSortParam()
    {
        var repoType = typeof(PAMS.Domain.Repositories.IAllocationRepository);
        var method = repoType.GetMethod("GetFilteredAsync",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
        var parameters = method!.GetParameters();
        var sortParam = Array.Find(parameters, p => p.Name == "sort" && p.ParameterType == typeof(string));
        Assert.NotNull(sortParam);
    }

    [Fact(DisplayName = "Sort | IProjectRepository_GetFilteredAsync_ShouldHaveSortParam")]
    public void IProjectRepository_GetFilteredAsync_ShouldHaveSortParam()
    {
        var repoType = typeof(PAMS.Domain.Repositories.IProjectRepository);
        var method = repoType.GetMethod("GetFilteredAsync",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
        var parameters = method!.GetParameters();
        var sortParam = Array.Find(parameters, p => p.Name == "sort" && p.ParameterType == typeof(string));
        Assert.NotNull(sortParam);
    }

    [Fact(DisplayName = "Sort | IAccountRepository_GetFilteredAsync_ShouldHaveSortParam")]
    public void IAccountRepository_GetFilteredAsync_ShouldHaveSortParam()
    {
        var repoType = typeof(PAMS.Domain.Repositories.IAccountRepository);
        var method = repoType.GetMethod("GetFilteredAsync",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
        var parameters = method!.GetParameters();
        var sortParam = Array.Find(parameters, p => p.Name == "sort" && p.ParameterType == typeof(string));
        Assert.NotNull(sortParam);
    }

    [Fact(DisplayName = "Sort | IEmployeeRepository_GetFilteredAsync_ShouldHaveSortParam")]
    public void IEmployeeRepository_GetFilteredAsync_ShouldHaveSortParam()
    {
        var repoType = typeof(PAMS.Domain.Repositories.IEmployeeRepository);
        var method = repoType.GetMethod("GetFilteredAsync",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
        var parameters = method!.GetParameters();
        var sortParam = Array.Find(parameters, p => p.Name == "sort" && p.ParameterType == typeof(string));
        Assert.NotNull(sortParam);
    }

    // ── GetFilteredAllAsync for export ───────────────────────────────────

    [Fact(DisplayName = "Export | IAllocationRepository_ShouldHaveGetFilteredAllAsyncMethod")]
    public void IAllocationRepository_ShouldHaveGetFilteredAllAsyncMethod()
    {
        var repoType = typeof(PAMS.Domain.Repositories.IAllocationRepository);
        var method = repoType.GetMethod("GetFilteredAllAsync",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
    }

    [Fact(DisplayName = "Export | IProjectRepository_ShouldHaveGetFilteredAllAsyncMethod")]
    public void IProjectRepository_ShouldHaveGetFilteredAllAsyncMethod()
    {
        var repoType = typeof(PAMS.Domain.Repositories.IProjectRepository);
        var method = repoType.GetMethod("GetFilteredAllAsync",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
    }

    [Fact(DisplayName = "Export | IAccountRepository_ShouldHaveGetFilteredAllAsyncMethod")]
    public void IAccountRepository_ShouldHaveGetFilteredAllAsyncMethod()
    {
        var repoType = typeof(PAMS.Domain.Repositories.IAccountRepository);
        var method = repoType.GetMethod("GetFilteredAllAsync",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
    }

    [Fact(DisplayName = "Export | IEmployeeRepository_ShouldHaveGetFilteredAllAsyncMethod")]
    public void IEmployeeRepository_ShouldHaveGetFilteredAllAsyncMethod()
    {
        var repoType = typeof(PAMS.Domain.Repositories.IEmployeeRepository);
        var method = repoType.GetMethod("GetFilteredAllAsync",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
    }
}
