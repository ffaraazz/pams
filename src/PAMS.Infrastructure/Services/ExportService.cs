using System.Reflection;
using ClosedXML.Excel;
using PAMS.Application.DTOs.Accounts;
using PAMS.Application.DTOs.Allocations;
using PAMS.Application.DTOs.Employees;
using PAMS.Application.DTOs.Projects;
using PAMS.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PAMS.Infrastructure.Services;

public sealed class ExportService : IExportService
{
    private static byte[]? _companyLogoCache;
    private static string? _appLogoSvgCache;

    private static byte[] GetCompanyLogo()
    {
        if (_companyLogoCache is not null) return _companyLogoCache;
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("PAMS.Infrastructure.Assets.nexturn-logo-black.png")
            ?? throw new InvalidOperationException("Embedded resource 'nexturn-logo-black.png' not found.");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        _companyLogoCache = ms.ToArray();
        return _companyLogoCache;
    }

    private static string GetAppLogoSvg()
    {
        if (_appLogoSvgCache is not null) return _appLogoSvgCache;
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("PAMS.Infrastructure.Assets.nexflow-logo.svg")
            ?? throw new InvalidOperationException("Embedded resource 'nexflow-logo.svg' not found.");
        using var reader = new StreamReader(stream);
        _appLogoSvgCache = reader.ReadToEnd();
        return _appLogoSvgCache;
    }

    // ── Column registries ─────────────────────────────────────────────────

    private static readonly Dictionary<string, (string Label, Func<AllocationDetailResponse, object?> Get)> AllocationColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["allocationId"] = ("Allocation ID", d => d.AllocationId),
        ["employeeId"] = ("Employee ID", d => d.EmployeeId),
        ["empCode"] = ("Emp Code", d => d.EmpCode),
        ["employeeName"] = ("Employee Name", d => d.EmployeeName),
        ["projectId"] = ("Project ID", d => d.ProjectId),
        ["projectCode"] = ("Project Code", d => d.ProjectCode),
        ["projectName"] = ("Project Name", d => d.ProjectName),
        ["percentage"] = ("Percentage", d => d.Percentage),
        ["fromDate"] = ("From Date", d => d.FromDate.ToString("yyyy-MM-dd")),
        ["toDate"] = ("To Date", d => d.ToDate?.ToString("yyyy-MM-dd")),
        ["createdAt"] = ("Created At", d => d.CreatedAt.ToString("yyyy-MM-dd")),
        ["projectRole"] = ("Project Role", d => d.ProjectRole),
        ["billable"] = ("Billable", d => d.Billable),
        ["projectBillable"] = ("Project Billable", d => d.ProjectBillable),
        ["accountCode"] = ("Account Code", d => d.AccountCode),
        ["accountName"] = ("Account Name", d => d.AccountName),
        ["status"] = ("Status", d => d.Status),
        ["updatedAt"] = ("Updated At", d => d.UpdatedAt.ToString("yyyy-MM-dd")),
    };

    private static readonly Dictionary<string, (string Label, Func<ProjectSummaryResponse, object?> Get)> ProjectColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["projectId"] = ("Project ID", d => d.ProjectId),
        ["projectCode"] = ("Project Code", d => d.ProjectCode),
        ["projectName"] = ("Project Name", d => d.ProjectName),
        ["accountId"] = ("Account ID", d => d.AccountId),
        ["accountCode"] = ("Account Code", d => d.AccountCode),
        ["accountName"] = ("Account Name", d => d.AccountName),
        ["projectManagerId"] = ("PM ID", d => d.ProjectManagerId),
        ["projectManagerEmpCode"] = ("PM Emp Code", d => d.ProjectManagerEmpCode),
        ["projectManagerName"] = ("PM Name", d => d.ProjectManagerName),
        ["status"] = ("Status", d => d.Status.ToString()),
        ["billable"] = ("Billable", d => d.Billable),
        ["isActive"] = ("Active", d => d.IsActive),
        ["startDate"] = ("Start Date", d => d.StartDate.ToString("yyyy-MM-dd")),
        ["endDate"] = ("End Date", d => d.EndDate?.ToString("yyyy-MM-dd")),
        ["resourceCount"] = ("Resources", d => d.ResourceCount),
    };

    private static readonly Dictionary<string, (string Label, Func<AccountSummaryResponse, object?> Get)> AccountColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["accountId"] = ("Account ID", d => d.AccountId),
        ["accountCode"] = ("Account Code", d => d.AccountCode),
        ["accountName"] = ("Account Name", d => d.AccountName),
        ["accountType"] = ("Account Type", d => d.AccountType.ToString()),
        ["isActive"] = ("Active", d => d.IsActive),
        ["totalActiveProjects"] = ("Active Projects", d => d.TotalActiveProjects),
        ["totalInactiveProjects"] = ("Inactive Projects", d => d.TotalInactiveProjects),
        ["totalActiveEmployees"] = ("Active Employees", d => d.TotalActiveEmployees),
        ["totalInactiveEmployees"] = ("Inactive Employees", d => d.TotalInactiveEmployees),
    };

    private static readonly Dictionary<string, (string Label, Func<EmployeeSummaryResponse, object?> Get)> EmployeeColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["employeeId"] = ("Employee ID", d => d.EmployeeId),
        ["empCode"] = ("Emp Code", d => d.EmpCode),
        ["fullName"] = ("Full Name", d => d.FullName),
        ["designation"] = ("Designation", d => d.Designation),
        ["role"] = ("Role", d => d.Role.ToString()),
        ["isActive"] = ("Active", d => d.IsActive),
        ["availabilityPercentage"] = ("Availability %", d => d.AvailabilityPercentage),
        ["allocationStatus"] = ("Allocation Status", d => d.AllocationStatus.ToString()),
        ["skills"] = ("Skills", d => string.Join(", ", d.Skills)),
    };

    // Default column subsets (what to show when no columns map is provided)
    private static readonly string[] DefaultAllocationColumns =
        ["empCode", "employeeName", "projectCode", "projectName", "accountCode", "percentage", "fromDate", "toDate", "status", "billable", "projectRole"];
    private static readonly string[] DefaultProjectColumns =
        ["projectCode", "projectName", "accountCode", "accountName", "projectManagerEmpCode", "projectManagerName", "status", "billable", "isActive", "startDate", "endDate", "resourceCount"];
    private static readonly string[] DefaultAccountColumns =
        ["accountCode", "accountName", "accountType", "isActive", "totalActiveProjects", "totalInactiveProjects"];
    private static readonly string[] DefaultEmployeeColumns =
        ["empCode", "fullName", "designation", "role", "isActive", "availabilityPercentage", "allocationStatus", "skills"];

    // ── Public methods ─────────────────────────────────────────────────────

    public async Task<ExportResult> GenerateAllocationsAsync(
        IReadOnlyList<AllocationDetailResponse> data, string format,
        Dictionary<string, string>? columns = null, CancellationToken ct = default)
    {
        var (headers, rows) = BuildColumnar(data, AllocationColumns, DefaultAllocationColumns, columns);
        return await GenerateAsync("Allocations", headers, rows, format);
    }

    public async Task<ExportResult> GenerateProjectsAsync(
        IReadOnlyList<ProjectSummaryResponse> data, string format,
        Dictionary<string, string>? columns = null, CancellationToken ct = default)
    {
        var (headers, rows) = BuildColumnar(data, ProjectColumns, DefaultProjectColumns, columns);
        return await GenerateAsync("Projects", headers, rows, format);
    }

    public async Task<ExportResult> GenerateAccountsAsync(
        IReadOnlyList<AccountSummaryResponse> data, string format,
        Dictionary<string, string>? columns = null, CancellationToken ct = default)
    {
        var (headers, rows) = BuildColumnar(data, AccountColumns, DefaultAccountColumns, columns);
        return await GenerateAsync("Accounts", headers, rows, format);
    }

    public async Task<ExportResult> GenerateEmployeesAsync(
        IReadOnlyList<EmployeeSummaryResponse> data, string format,
        Dictionary<string, string>? columns = null, CancellationToken ct = default)
    {
        var (headers, rows) = BuildColumnar(data, EmployeeColumns, DefaultEmployeeColumns, columns);
        return await GenerateAsync("Employees", headers, rows, format);
    }

    // ── Core logic ─────────────────────────────────────────────────────────

    private static (string[] Headers, List<object?[]> Rows) BuildColumnar<T>(
        IReadOnlyList<T> data,
        Dictionary<string, (string Label, Func<T, object?> Get)> registry,
        string[] defaultFields,
        Dictionary<string, string>? userColumns)
    {
        // Determine which columns to include and their display labels
        List<(string Label, Func<T, object?> Get)> selected;

        if (userColumns is not null && userColumns.Count > 0)
        {
            // User specified columns — use their order and labels, filter to valid fields
            selected = new List<(string, Func<T, object?>)>();
            foreach (var (field, label) in userColumns)
            {
                if (registry.TryGetValue(field, out var entry))
                    selected.Add((label, entry.Get));
            }
        }
        else
        {
            // Use defaults
            selected = defaultFields
                .Where(f => registry.ContainsKey(f))
                .Select(f => (registry[f].Label, registry[f].Get))
                .ToList();
        }

        // Fallback: if user-provided columns resulted in no valid matches, use defaults
        if (selected.Count == 0)
        {
            selected = defaultFields
                .Where(f => registry.ContainsKey(f))
                .Select(f => (registry[f].Label, registry[f].Get))
                .ToList();
        }

        var headers = selected.Select(s => s.Label).ToArray();
        var rows = data.Select(d => selected.Select(s => s.Get(d)).ToArray()).ToList();
        return (headers, rows);
    }

    private static async Task<ExportResult> GenerateAsync(string title, string[] headers, List<object?[]> rows, string format)
    {
        return format.ToLowerInvariant() switch
        {
            "xlsx" => GenerateXlsx(title, headers, rows),
            "pdf" => GeneratePdf(title, headers, rows),
            _ => throw new ArgumentException($"Unsupported export format: {format}")
        };
    }

    private static ExportResult GenerateXlsx(string title, string[] headers, List<object?[]> rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(title);

        // Header row
        for (int col = 0; col < headers.Length; col++)
        {
            var cell = worksheet.Cell(1, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#5048e5");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        // Data rows
        for (int row = 0; row < rows.Count; row++)
        {
            for (int col = 0; col < rows[row].Length; col++)
            {
                var value = rows[row][col];
                var cell = worksheet.Cell(row + 2, col + 1);
                if (value is null) cell.Value = Blank.Value;
                else if (value is int i) cell.Value = i;
                else if (value is bool b) cell.Value = b ? "Yes" : "No";
                else if (value is Guid g) cell.Value = g.ToString();
                else cell.Value = value.ToString();
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
        }

        worksheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return new ExportResult(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"{title}.xlsx");
    }

    private static ExportResult GeneratePdf(string title, string[] headers, List<object?[]> rows)
    {
        var companyLogo = GetCompanyLogo();
        var appLogoSvg = GetAppLogoSvg();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(8));

                // ── Header: App logo + name (left) | Company logo (right) ──
                page.Header().Column(headerCol =>
                {
                    headerCol.Item().PaddingBottom(10).Row(row =>
                    {
                        // Left: App logo + NexFlow + tagline
                        row.RelativeItem().Row(left =>
                        {
                            left.ConstantItem(30).AlignMiddle().Svg(appLogoSvg).FitArea();
                            left.ConstantItem(8); // spacer
                            left.RelativeItem().AlignMiddle().Column(col =>
                            {
                                col.Item().Text("NexFlow").Bold().FontSize(14).FontColor(Colors.Indigo.Darken2);
                                col.Item().Text("Enterprise Resource Planning").FontSize(8).FontColor(Colors.Grey.Darken1);
                            });
                        });

                        // Right: Company logo
                        row.ConstantItem(120).AlignRight().AlignMiddle()
                            .Image(companyLogo).FitArea();
                    });

                    // Title line (separate row below logos)
                    headerCol.Item().PaddingBottom(8)
                        .Text(title).SemiBold().FontSize(12).FontColor(Colors.Grey.Darken3);
                });

                // ── Table content ─────────────────────────────────
                page.Content().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        for (int i = 0; i < headers.Length; i++)
                            columns.RelativeColumn();
                    });

                    // Header row
                    foreach (var header in headers)
                    {
                        table.Cell()
                            .Background(Colors.Indigo.Darken2)
                            .Border(0.5f)
                            .BorderColor(Colors.Grey.Darken2)
                            .Padding(5)
                            .Text(header).SemiBold().FontColor(Colors.White);
                    }

                    // Data rows with alternating background
                    for (int r = 0; r < rows.Count; r++)
                    {
                        var bg = r % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                        foreach (var cell in rows[r])
                        {
                            var cellText = cell?.ToString() ?? "";
                            if (cell is bool bVal) cellText = bVal ? "Yes" : "No";
                            table.Cell()
                                .Background(bg)
                                .Border(0.5f)
                                .BorderColor(Colors.Grey.Lighten1)
                                .Padding(5)
                                .Text(cellText);
                        }
                    }
                });

                // ── Footer ────────────────────────────────────────
                page.Footer().Row(footer =>
                {
                    footer.RelativeItem().AlignLeft().Text(text =>
                    {
                        text.Span("Generated on ").FontSize(7).FontColor(Colors.Grey.Darken1);
                        text.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm")).FontSize(7).FontColor(Colors.Grey.Darken1);
                    });
                    footer.RelativeItem().AlignCenter().Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                    footer.RelativeItem().AlignRight()
                        .Text("NexFlow — Powered by NexTurn").FontSize(7).FontColor(Colors.Grey.Darken1);
                });
            });
        });

        var bytes = document.GeneratePdf();
        return new ExportResult(bytes, "application/pdf", $"{title}.pdf");
    }
}
