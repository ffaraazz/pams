using PAMS.Application.DTOs.Allocations;
using PAMS.Application.DTOs.Projects;
using PAMS.Application.DTOs.Accounts;
using PAMS.Application.DTOs.Employees;

namespace PAMS.Application.Interfaces;

public record ExportResult(byte[] FileBytes, string ContentType, string FileName);

public interface IExportService
{
    Task<ExportResult> GenerateAllocationsAsync(IReadOnlyList<AllocationDetailResponse> data, string format, Dictionary<string, string>? columns = null, CancellationToken ct = default);
    Task<ExportResult> GenerateProjectsAsync(IReadOnlyList<ProjectSummaryResponse> data, string format, Dictionary<string, string>? columns = null, CancellationToken ct = default);
    Task<ExportResult> GenerateAccountsAsync(IReadOnlyList<AccountSummaryResponse> data, string format, Dictionary<string, string>? columns = null, CancellationToken ct = default);
    Task<ExportResult> GenerateEmployeesAsync(IReadOnlyList<EmployeeSummaryResponse> data, string format, Dictionary<string, string>? columns = null, CancellationToken ct = default);
}
