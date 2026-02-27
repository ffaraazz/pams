using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAMS.Application.DTOs.Allocations;
using PAMS.Application.DTOs.Common;
using PAMS.Application.DTOs.Dashboard;
using PAMS.Application.Interfaces;
using PAMS.Domain.Enums;
using PAMS.Domain.Repositories;

namespace PAMS.API.Controllers;

/// <summary>
/// Dashboard views — project-centric and employee-centric allocation overviews.
/// </summary>
[ApiController]
[Route("api/v1/dashboard")]
[Authorize(Policy = "CanViewDashboard")]
[Tags("Dashboard")]
[Produces("application/json")]
public sealed class DashboardController : ControllerBase
{
    private readonly IAccountRepository _accountRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly IAllocationRepository _allocationRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly ICurrentUserService _currentUser;

    public DashboardController(
        IAccountRepository accountRepo,
        IProjectRepository projectRepo,
        IAllocationRepository allocationRepo,
        IEmployeeRepository employeeRepo,
        ICurrentUserService currentUser)
    {
        _accountRepo = accountRepo;
        _projectRepo = projectRepo;
        _allocationRepo = allocationRepo;
        _employeeRepo = employeeRepo;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Project View – all projects with their allocations (HR, PM).
    /// </summary>
    /// <remarks>
    /// Returns projects grouped by account, each with their current allocations.
    /// PMs see only their own projects. HR sees all.
    /// </remarks>
    /// <param name="fromDate">Start of date window (default: today).</param>
    /// <param name="toDate">End of date window.</param>
    /// <param name="includeEnded">Include ended (past) allocations.</param>
    /// <param name="accountCode">Filter to a specific account.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Projects grouped by account with allocations.</returns>
    [HttpGet("project-view")]
    [ProducesResponseType(typeof(ProjectViewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProjectViewResponse>> ProjectView(
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        [FromQuery] bool includeEnded = false,
        [FromQuery] string? accountCode = null,
        CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var effectiveFrom = fromDate ?? today;

        // Get accounts (optionally filtered)
        var accounts = await _accountRepo.GetFilteredAsync(
            null, true, null, 1, 1000, ct);

        if (!string.IsNullOrWhiteSpace(accountCode))
            accounts = accounts.Where(a =>
                string.Equals(a.AccountCode, accountCode, StringComparison.OrdinalIgnoreCase))
                .ToList();

        var accountGroups = new List<ProjectViewAccountGroup>();

        foreach (var account in accounts)
        {
            var projects = await _projectRepo.GetFilteredAsync(
                null, account.AccountCode, null, true, null, null, 1, 1000, ct);

            // PM scope: PM only sees their own projects
            if (_currentUser.Role == EmployeeRole.ProjectManager)
                projects = projects.Where(p => p.ProjectManagerId == _currentUser.EmployeeId).ToList();

            if (!projects.Any()) continue;

            var projectItems = new List<ProjectViewItem>();

            foreach (var project in projects)
            {
                var allocations = await _allocationRepo.GetByProjectAsync(project.Id, ct);

                var filtered = allocations.Where(a =>
                {
                    if (a.DeletedAt != null) return false;
                    if (!includeEnded && a.ToDate.HasValue && a.ToDate.Value < today) return false;
                    return true;
                }).ToList();

                var employee = project.ProjectManager;

                projectItems.Add(new ProjectViewItem
                {
                    ProjectId = project.Id,
                    ProjectCode = project.ProjectCode,
                    ProjectName = project.ProjectName,
                    Status = project.Status,
                    Billable = project.Billable,
                    ProjectManagerName = employee is not null
                        ? $"{employee.FirstName} {employee.LastName}" : null,
                    IsCurrentUserPM = project.ProjectManagerId == _currentUser.EmployeeId,
                    Allocations = filtered.Select(a => new AllocationDetailResponse
                    {
                        AllocationId = a.Id,
                        EmployeeId = a.EmployeeId,
                        EmpCode = a.Employee?.EmpCode ?? string.Empty,
                        EmployeeName = a.Employee is not null
                            ? $"{a.Employee.FirstName} {a.Employee.LastName}" : string.Empty,
                        ProjectId = a.ProjectId,
                        ProjectCode = project.ProjectCode,
                        ProjectName = project.ProjectName,
                        Percentage = a.Percentage,
                        FromDate = a.FromDate,
                        ToDate = a.ToDate,
                        CreatedAt = a.CreatedAt
                    }).ToList()
                });
            }

            accountGroups.Add(new ProjectViewAccountGroup
            {
                AccountId = account.Id,
                AccountCode = account.AccountCode,
                AccountName = account.AccountName,
                AccountType = account.AccountType,
                Projects = projectItems
            });
        }

        return Ok(new ProjectViewResponse
        {
            Accounts = accountGroups,
            FiltersApplied = new ProjectViewFilters
            {
                FromDate = fromDate,
                ToDate = toDate,
                IncludeEnded = includeEnded
            }
        });
    }

    /// <summary>
    /// Employee View – all employees with their allocations (HR, PM).
    /// </summary>
    /// <remarks>
    /// Returns a paginated list of employees with their allocations and availability.
    /// Supports search, bench-only filter, and date window for availability computation.
    /// </remarks>
    /// <param name="q">Search query (name, empCode, skill).</param>
    /// <param name="benchOnly">Return only employees with 0% allocation.</param>
    /// <param name="windowFrom">Start of availability window (default: today).</param>
    /// <param name="windowTo">End of availability window.</param>
    /// <param name="includeEnded">Include ended (past) allocations.</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="limit">Items per page (default: 10, max: 100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated list of employees with allocations.</returns>
    [HttpGet("employee-view")]
    [ProducesResponseType(typeof(EmployeeViewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EmployeeViewResponse>> EmployeeView(
        [FromQuery] string? q = null,
        [FromQuery] bool benchOnly = false,
        [FromQuery] DateOnly? windowFrom = null,
        [FromQuery] DateOnly? windowTo = null,
        [FromQuery] bool includeEnded = false,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 100);
        page = Math.Max(page, 1);

        var employees = await _employeeRepo.GetFilteredAsync(
            q, null, benchOnly, null, true, windowFrom, windowTo, page, limit, ct);
        var totalRecords = await _employeeRepo.GetFilteredCountAsync(
            q, null, benchOnly, null, true, windowFrom, windowTo, ct);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var data = new List<EmployeeViewItem>();

        foreach (var e in employees)
        {
            var allocations = await _allocationRepo.GetByEmployeeAsync(e.Id, ct);

            var filtered = allocations.Where(a =>
            {
                if (a.DeletedAt != null) return false;
                if (!includeEnded && a.ToDate.HasValue && a.ToDate.Value < today) return false;
                return true;
            }).ToList();

            var activeAllocations = allocations
                .Where(a => a.DeletedAt == null && a.FromDate <= today && (a.ToDate == null || a.ToDate >= today))
                .ToList();
            var totalPct = activeAllocations.Sum(a => a.Percentage);
            var availability = Math.Max(0, 100 - totalPct);

            data.Add(new EmployeeViewItem
            {
                EmployeeId = e.Id,
                EmpCode = e.EmpCode,
                FullName = $"{e.FirstName} {e.LastName}",
                Designation = e.Designation,
                Role = e.Role,
                AvailabilityPercentage = availability,
                AllocationStatus = totalPct == 0 ? AllocationStatus.Bench
                    : totalPct >= 100 ? AllocationStatus.Full
                    : AllocationStatus.Partial,
                Skills = e.EmployeeSkills.Select(es => es.Skill?.SkillName ?? string.Empty).ToList(),
                Allocations = filtered.Select(a => new AllocationDetailResponse
                {
                    AllocationId = a.Id,
                    EmployeeId = a.EmployeeId,
                    EmpCode = e.EmpCode,
                    EmployeeName = $"{e.FirstName} {e.LastName}",
                    ProjectId = a.ProjectId,
                    ProjectCode = a.Project?.ProjectCode ?? string.Empty,
                    ProjectName = a.Project?.ProjectName ?? string.Empty,
                    Percentage = a.Percentage,
                    FromDate = a.FromDate,
                    ToDate = a.ToDate,
                    CreatedAt = a.CreatedAt
                }).ToList()
            });
        }

        return Ok(new EmployeeViewResponse
        {
            Data = data,
            Pagination = PaginationMeta.Create(page, limit, totalRecords)
        });
    }
}
