using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PAMS.IntegrationTests.Infrastructure;

/// <summary>
/// Test authentication handler that reads role, empCode and employeeId from
/// custom HTTP headers, producing a ClaimsPrincipal for the request pipeline.
///
/// Headers:
///   X-Test-Role        → ClaimTypes.Role (maps to HR, ProjectManager, Staff)
///   X-Test-EmpCode     → "empCode" custom claim
///   X-Test-EmployeeId  → ClaimTypes.NameIdentifier (sub)
/// </summary>
public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Default: if no role header is sent, authenticate as HR
        var role = Request.Headers["X-Test-Role"].FirstOrDefault() ?? "HR";
        var empCode = Request.Headers["X-Test-EmpCode"].FirstOrDefault() ?? "EMP-001";
        var employeeId = Request.Headers["X-Test-EmployeeId"].FirstOrDefault()
            ?? Guid.Empty.ToString();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, employeeId),
            new(ClaimTypes.Name, empCode),
            new("empCode", empCode),
            new(ClaimTypes.Role, role),
            new("preferred_username", empCode)
        };

        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
