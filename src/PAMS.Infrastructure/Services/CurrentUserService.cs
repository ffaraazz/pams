using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PAMS.Application.Interfaces;
using PAMS.Domain.Enums;

namespace PAMS.Infrastructure.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid EmployeeId
    {
        get
        {
            var sub = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? _httpContextAccessor.HttpContext?.User.FindFirstValue("sub");
            return sub is not null ? Guid.Parse(sub) : Guid.Empty;
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
