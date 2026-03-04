using PAMS.Domain.Enums;

namespace PAMS.Application.Interfaces;

/// <summary>
/// Provides the current authenticated user's identity.
/// Authentication is JWT-based (IdP-agnostic); authorization role is resolved
/// from the database Employee.Role column, not from JWT claims.
/// </summary>
public interface ICurrentUserService
{
    Guid EmployeeId { get; }
    string EmpCode { get; }
    EmployeeRole Role { get; }
}
