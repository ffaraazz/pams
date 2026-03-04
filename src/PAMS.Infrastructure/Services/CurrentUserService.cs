using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PAMS.Application.Interfaces;
using PAMS.Domain.Enums;
using PAMS.Infrastructure.Persistence;

namespace PAMS.Infrastructure.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly PamsDbContext _dbContext;
    private bool _resolved;
    private Guid _cachedEmployeeId;
    private EmployeeRole _cachedRole = EmployeeRole.Staff;
    private string? _cachedEmpCode;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, PamsDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
    }

    public Guid EmployeeId
    {
        get
        {
            EnsureResolved();
            return _cachedEmployeeId;
        }
    }

    public string EmpCode
    {
        get
        {
            if (_cachedEmpCode is not null)
                return _cachedEmpCode;

            _cachedEmpCode = _httpContextAccessor.HttpContext?.User.FindFirstValue("empCode")
                ?? string.Empty;
            return _cachedEmpCode;
        }
    }

    /// <summary>
    /// Role resolved from the database Employee.Role column — NOT from JWT claims.
    /// This makes authorization IdP-agnostic.
    /// </summary>
    public EmployeeRole Role
    {
        get
        {
            EnsureResolved();
            return _cachedRole;
        }
    }

    /// <summary>
    /// Resolves EmployeeId and Role from the database in a single query.
    /// Cached for the lifetime of the scoped service (one HTTP request).
    /// </summary>
    private void EnsureResolved()
    {
        if (_resolved) return;
        _resolved = true;

        var empCode = EmpCode;
        if (string.IsNullOrEmpty(empCode))
        {
            _cachedEmployeeId = Guid.Empty;
            _cachedRole = EmployeeRole.Staff;
            return;
        }

        var employee = _dbContext.Employees
            .Where(e => e.EmpCode == empCode)
            .Select(e => new { e.Id, e.Role })
            .FirstOrDefault();

        if (employee is not null)
        {
            _cachedEmployeeId = employee.Id;
            _cachedRole = employee.Role;
        }
        else
        {
            _cachedEmployeeId = Guid.Empty;
            _cachedRole = EmployeeRole.Staff;
        }
    }
}
