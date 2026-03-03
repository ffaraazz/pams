using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PAMS.Application.Interfaces;
using PAMS.Domain.Enums;
using PAMS.Infrastructure.Persistence;

namespace PAMS.Infrastructure.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly PamsDbContext _dbContext;
    private Guid? _cachedEmployeeId;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, PamsDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
    }

    public Guid EmployeeId
    {
        get
        {
            if (_cachedEmployeeId.HasValue)
                return _cachedEmployeeId.Value;

            // First try the JWT sub claim (if Keycloak is configured to set sub = database employee ID)
            var sub = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? _httpContextAccessor.HttpContext?.User.FindFirstValue("sub");

            if (sub is not null && Guid.TryParse(sub, out var subGuid))
            {
                // Verify this GUID exists in the employees table
                var exists = _dbContext.Employees.Any(e => e.Id == subGuid);
                if (exists)
                {
                    _cachedEmployeeId = subGuid;
                    return subGuid;
                }
            }

            // Fallback: resolve employee ID from empCode claim (Keycloak custom attribute)
            var empCode = EmpCode;
            if (!string.IsNullOrEmpty(empCode))
            {
                var employeeId = _dbContext.Employees
                    .Where(e => e.EmpCode == empCode)
                    .Select(e => e.Id)
                    .FirstOrDefault();

                _cachedEmployeeId = employeeId;
                return employeeId;
            }

            _cachedEmployeeId = Guid.Empty;
            return Guid.Empty;
        }
    }

    public string EmpCode
    {
        get
        {
            return _httpContextAccessor.HttpContext?.User.FindFirstValue("empCode")
                ?? string.Empty;
        }
    }

    public EmployeeRole Role
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user is null) return EmployeeRole.Staff;

            // Try Keycloak realm_access.roles first, then standard role claims
            if (user.IsInRole("HR")) return EmployeeRole.HR;
            if (user.IsInRole("ProjectManager")) return EmployeeRole.ProjectManager;
            return EmployeeRole.Staff;
        }
    }
}
