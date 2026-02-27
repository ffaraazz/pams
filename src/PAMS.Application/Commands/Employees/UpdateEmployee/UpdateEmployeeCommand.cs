using MediatR;
using PAMS.Domain.Enums;

namespace PAMS.Application.Commands.Employees;

/// <summary>
/// Command to update an employee (FR-008). EmpCode is immutable.
/// </summary>
public sealed record UpdateEmployeeCommand : IRequest<Unit>
{
    public required string EmpCode { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public required string Designation { get; init; }
    public required EmployeeRole Role { get; init; }
    public string? ReportsToEmpCode { get; init; }
    public List<Guid> SkillIds { get; init; } = [];
    public bool? IsActive { get; init; }
}
