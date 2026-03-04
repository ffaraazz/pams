using Microsoft.AspNetCore.Authorization;
using PAMS.Domain.Enums;

namespace PAMS.API.Authorization;

/// <summary>
/// Authorization requirement that checks if the user's DB-stored EmployeeRole
/// matches any of the allowed roles. This makes authorization IdP-agnostic.
/// </summary>
public sealed class DbRoleRequirement : IAuthorizationRequirement
{
    public EmployeeRole[] AllowedRoles { get; }

    public DbRoleRequirement(params EmployeeRole[] allowedRoles)
    {
        AllowedRoles = allowedRoles;
    }
}
