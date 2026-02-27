using MediatR;
using PAMS.Domain.Enums;

namespace PAMS.Application.Commands.Employees;

/// <summary>
/// Command to create a new employee (FR-007).
/// </summary>
public sealed record CreateEmployeeCommand : IRequest<Guid>
{
    public required string EmpCode { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public required EmployeeRole Role { get; init; }
    public required string Designation { get; init; }
    public string? ReportsToEmpCode { get; init; }
    public List<Guid> SkillIds { get; init; } = [];
}
