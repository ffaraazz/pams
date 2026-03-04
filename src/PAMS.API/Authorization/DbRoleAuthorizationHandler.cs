using Microsoft.AspNetCore.Authorization;
using PAMS.Application.Interfaces;

namespace PAMS.API.Authorization;

/// <summary>
/// Handles <see cref="DbRoleRequirement"/> by checking the user's role
/// from the database via <see cref="ICurrentUserService"/>.
/// No JWT role claims are inspected — the Employee.Role column is the single source of truth.
/// </summary>
public sealed class DbRoleAuthorizationHandler : AuthorizationHandler<DbRoleRequirement>
{
    private readonly ICurrentUserService _currentUser;

    public DbRoleAuthorizationHandler(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DbRoleRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        if (requirement.AllowedRoles.Contains(_currentUser.Role))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
