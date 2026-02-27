using PAMS.Domain.Enums;

namespace PAMS.Application.Interfaces;

/// <summary>
/// Provides the current authenticated user's identity extracted from the JWT token.
/// </summary>
public interface ICurrentUserService
{
    Guid EmployeeId { get; }
    string EmpCode { get; }
    EmployeeRole Role { get; }
}
