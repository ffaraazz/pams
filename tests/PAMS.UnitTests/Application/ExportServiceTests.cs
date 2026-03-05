using System.Reflection;
using PAMS.Application.DTOs.Allocations;
using PAMS.Application.DTOs.Projects;
using PAMS.Application.DTOs.Accounts;
using PAMS.Application.DTOs.Employees;
using PAMS.Application.Interfaces;

namespace PAMS.UnitTests.Application;

/// <summary>
/// TDD Red Phase — verifies that IExportService interface and ExportResult record exist
/// with the expected members for the PDF/XLS export feature.
/// These tests will NOT COMPILE because IExportService and ExportResult do not exist yet.
/// </summary>
public sealed class ExportServiceTests
{
    [Fact(DisplayName = "Export | IExportService_ShouldExist")]
    public void IExportService_ShouldExist()
    {
        var serviceType = typeof(IExportService);
        Assert.NotNull(serviceType);
    }

    [Fact(DisplayName = "Export | IExportService_ShouldHaveGenerateAllocationsAsync")]
    public void IExportService_ShouldHaveGenerateAllocationsAsync()
    {
        var serviceType = typeof(IExportService);
        var method = serviceType.GetMethod("GenerateAllocationsAsync",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
        var parameters = method!.GetParameters();
        Assert.Equal(4, parameters.Length);
        Assert.Equal(typeof(IReadOnlyList<AllocationDetailResponse>), parameters[0].ParameterType);
        Assert.Equal(typeof(string), parameters[1].ParameterType);
        Assert.Equal(typeof(Dictionary<string, string>), parameters[2].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[3].ParameterType);
    }

    [Fact(DisplayName = "Export | IExportService_ShouldHaveGenerateProjectsAsync")]
    public void IExportService_ShouldHaveGenerateProjectsAsync()
    {
        var serviceType = typeof(IExportService);
        var method = serviceType.GetMethod("GenerateProjectsAsync",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
        var parameters = method!.GetParameters();
        Assert.Equal(4, parameters.Length);
        Assert.Equal(typeof(IReadOnlyList<ProjectSummaryResponse>), parameters[0].ParameterType);
        Assert.Equal(typeof(string), parameters[1].ParameterType);
        Assert.Equal(typeof(Dictionary<string, string>), parameters[2].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[3].ParameterType);
    }

    [Fact(DisplayName = "Export | IExportService_ShouldHaveGenerateAccountsAsync")]
    public void IExportService_ShouldHaveGenerateAccountsAsync()
    {
        var serviceType = typeof(IExportService);
        var method = serviceType.GetMethod("GenerateAccountsAsync",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
        var parameters = method!.GetParameters();
        Assert.Equal(4, parameters.Length);
        Assert.Equal(typeof(IReadOnlyList<AccountSummaryResponse>), parameters[0].ParameterType);
        Assert.Equal(typeof(string), parameters[1].ParameterType);
        Assert.Equal(typeof(Dictionary<string, string>), parameters[2].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[3].ParameterType);
    }

    [Fact(DisplayName = "Export | IExportService_ShouldHaveGenerateEmployeesAsync")]
    public void IExportService_ShouldHaveGenerateEmployeesAsync()
    {
        var serviceType = typeof(IExportService);
        var method = serviceType.GetMethod("GenerateEmployeesAsync",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
        var parameters = method!.GetParameters();
        Assert.Equal(4, parameters.Length);
        Assert.Equal(typeof(IReadOnlyList<EmployeeSummaryResponse>), parameters[0].ParameterType);
        Assert.Equal(typeof(string), parameters[1].ParameterType);
        Assert.Equal(typeof(Dictionary<string, string>), parameters[2].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[3].ParameterType);
    }

    [Fact(DisplayName = "Export | ExportResult_ShouldHaveExpectedProperties")]
    public void ExportResult_ShouldHaveExpectedProperties()
    {
        var resultType = typeof(ExportResult);

        Assert.NotNull(resultType);

        var fileBytesProperty = resultType.GetProperty("FileBytes");
        Assert.NotNull(fileBytesProperty);
        Assert.Equal(typeof(byte[]), fileBytesProperty!.PropertyType);

        var contentTypeProperty = resultType.GetProperty("ContentType");
        Assert.NotNull(contentTypeProperty);
        Assert.Equal(typeof(string), contentTypeProperty!.PropertyType);

        var fileNameProperty = resultType.GetProperty("FileName");
        Assert.NotNull(fileNameProperty);
        Assert.Equal(typeof(string), fileNameProperty!.PropertyType);
    }
}
