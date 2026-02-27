using PAMS.Domain.Enums;

namespace PAMS.UnitTests.Helpers;

/// <summary>
/// Test data builder for Allocation-related test scenarios.
/// Provides fluent construction of allocation overlap records used by
/// AllocationCapacityService and related domain services.
/// </summary>
public sealed class AllocationOverlapBuilder
{
    private Guid _employeeId = Guid.NewGuid();
    private Guid _projectId = Guid.NewGuid();
    private DateOnly _fromDate = DateOnly.FromDateTime(DateTime.Today);
    private DateOnly? _toDate;
    private int _percentage = 50;
    private Guid _allocatedById = Guid.NewGuid();

    public AllocationOverlapBuilder WithEmployeeId(Guid employeeId)
    {
        _employeeId = employeeId;
        return this;
    }

    public AllocationOverlapBuilder WithProjectId(Guid projectId)
    {
        _projectId = projectId;
        return this;
    }

    public AllocationOverlapBuilder WithFromDate(DateOnly fromDate)
    {
        _fromDate = fromDate;
        return this;
    }

    public AllocationOverlapBuilder WithToDate(DateOnly? toDate)
    {
        _toDate = toDate;
        return this;
    }

    public AllocationOverlapBuilder WithPercentage(int percentage)
    {
        _percentage = percentage;
        return this;
    }

    public AllocationOverlapBuilder WithAllocatedById(Guid allocatedById)
    {
        _allocatedById = allocatedById;
        return this;
    }

    public AllocationOverlapBuilder OpenEnded()
    {
        _toDate = null;
        return this;
    }

    public (Guid EmployeeId, Guid ProjectId, DateOnly FromDate, DateOnly? ToDate, int Percentage, Guid AllocatedById) Build()
    {
        return (_employeeId, _projectId, _fromDate, _toDate, _percentage, _allocatedById);
    }

    public static AllocationOverlapBuilder Default() => new();
}

/// <summary>
/// Provides pre-configured test data constants for consistent test scenarios.
/// </summary>
public static class TestData
{
    public static readonly Guid HrEmployeeId = Guid.NewGuid();
    public static readonly Guid PmEmployeeId = Guid.NewGuid();
    public static readonly Guid StaffEmployeeId = Guid.NewGuid();
    public static readonly Guid ProjectId = Guid.NewGuid();
    public static readonly Guid AccountId = Guid.NewGuid();

    public const string HrEmpCode = "HR001";
    public const string PmEmpCode = "PM001";
    public const string StaffEmpCode = "EMP001";
    public const string ProjectCode = "PRJ001";
    public const string AccountCode = "ACC001";

    public static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);
    public static readonly DateOnly Tomorrow = Today.AddDays(1);
    public static readonly DateOnly Yesterday = Today.AddDays(-1);
    public static readonly DateOnly NextMonth = Today.AddMonths(1);
    public static readonly DateOnly LastMonth = Today.AddMonths(-1);
}
