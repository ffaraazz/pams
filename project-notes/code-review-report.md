# PAMS — Code Review Report

---

## 1. Document Control

| Field       | Value                                                                             |
| ----------- | --------------------------------------------------------------------------------- |
| Project     | Project Allocation Management System (PAMS)                                       |
| Review Date | 2026-03-05                                                                        |
| Reviewer    | CodeReviewer (GitHub Copilot)                                                     |
| Scope       | Sort Parameter (all list endpoints) + Export APIs (PDF/XLSX on all 4 controllers) |
| Build       | 0 errors, 0 warnings, 231/231 unit tests pass                                     |
| Verdict     | **PASS WITH OBSERVATIONS**                                                        |

### 1.1 Files Reviewed

| #   | File                                                                       | Status   | Change Summary                                                          |
| --- | -------------------------------------------------------------------------- | -------- | ----------------------------------------------------------------------- |
| 1   | `src/PAMS.Application/Helpers/SortHelper.cs`                               | NEW      | Static `Parse()` method with field whitelist, case-insensitive lookup   |
| 2   | `src/PAMS.Application/Interfaces/IExportService.cs`                        | NEW      | Interface + `ExportResult` record in Application layer                  |
| 3   | `src/PAMS.Domain/Repositories/IAllocationRepository.cs`                    | MODIFIED | Added `string? sort` to `GetFilteredAsync`, added `GetFilteredAllAsync` |
| 4   | `src/PAMS.Domain/Repositories/IProjectRepository.cs`                       | MODIFIED | Same: sort param + `GetFilteredAllAsync`                                |
| 5   | `src/PAMS.Domain/Repositories/IAccountRepository.cs`                       | MODIFIED | Same: sort param + `GetFilteredAllAsync`                                |
| 6   | `src/PAMS.Domain/Repositories/IEmployeeRepository.cs`                      | MODIFIED | Same: sort param + `GetFilteredAllAsync`                                |
| 7   | `src/PAMS.Infrastructure/Persistence/Repositories/AllocationRepository.cs` | MODIFIED | `SortFields` dict + `ApplySort` + `GetFilteredAllAsync` impl            |
| 8   | `src/PAMS.Infrastructure/Persistence/Repositories/ProjectRepository.cs`    | MODIFIED | Same pattern                                                            |
| 9   | `src/PAMS.Infrastructure/Persistence/Repositories/AccountRepository.cs`    | MODIFIED | Same pattern                                                            |
| 10  | `src/PAMS.Infrastructure/Persistence/Repositories/EmployeeRepository.cs`   | MODIFIED | Same pattern                                                            |
| 11  | `src/PAMS.Infrastructure/Services/ExportService.cs`                        | NEW      | ClosedXML (xlsx) + QuestPDF (pdf) implementation                        |
| 12  | `src/PAMS.Infrastructure/Extensions/InfrastructureServiceExtensions.cs`    | MODIFIED | `IExportService` → `ExportService` DI registration                      |
| 13  | `src/PAMS.Infrastructure/PAMS.Infrastructure.csproj`                       | MODIFIED | Added ClosedXML, QuestPDF, System.IO.Packaging packages                 |
| 14  | `src/PAMS.API/Controllers/AllocationsController.cs`                        | MODIFIED | `sort` query param on List + `[HttpGet("export")]` endpoint             |
| 15  | `src/PAMS.API/Controllers/ProjectsController.cs`                           | MODIFIED | Same: sort + export                                                     |
| 16  | `src/PAMS.API/Controllers/AccountsController.cs`                           | MODIFIED | Same: sort + export                                                     |
| 17  | `src/PAMS.API/Controllers/EmployeesController.cs`                          | MODIFIED | Same: sort + export                                                     |
| 18  | `src/PAMS.API/Program.cs`                                                  | MODIFIED | `QuestPDF.Settings.License = LicenseType.Community`                     |
| 19  | `Directory.Packages.props`                                                 | MODIFIED | ClosedXML 0.104.1, QuestPDF 2024.12.3, System.IO.Packaging 9.0.6        |
| 20  | `tests/PAMS.UnitTests/Application/SortHelperTests.cs`                      | NEW      | 7 tests for `SortHelper.Parse()` behaviour                              |
| 21  | `tests/PAMS.UnitTests/Application/SortEndpointTests.cs`                    | NEW      | 8 reflection tests verifying interface contracts                        |
| 22  | `tests/PAMS.UnitTests/Application/ExportServiceTests.cs`                   | NEW      | 6 reflection tests verifying `IExportService`/`ExportResult` contract   |

---

## 2. Executive Summary

This changeset adds two features:

1. **Sort Parameter** — All four list endpoints (`GET /allocations`, `/projects`, `/accounts`, `/employees`) now accept an optional `sort` query parameter (e.g. `sort=-fromDate` for descending). Each repository implements its own whitelist-based `ApplySort` with a static `SortFields` dictionary keyed by `StringComparer.OrdinalIgnoreCase`. Invalid or missing sort values silently fall back to a sensible default sort order. A centralized `SortHelper.Parse()` utility exists in the Application layer but is **not used** by any production code path — only by unit tests.

2. **Export APIs** — Four new `[HttpGet("export")]` endpoints produce PDF or XLSX downloads. The controller accepts `ext=pdf|xls`, converts `xls` → `xlsx` internally, and delegates to `IExportService`. The implementation uses ClosedXML for spreadsheets and QuestPDF (Community license) for PDFs. Data is fetched via new `GetFilteredAllAsync` repository methods (no pagination) and materialized fully in-memory.

**Key findings:**

- **Medium (Performance/DoS):** `GetFilteredAllAsync` has no row cap — an unfiltered export on a large table loads all records into memory, generates a full XLSX/PDF byte array, and returns it synchronously. No circuit breaker or streaming.
- **Medium (Dead Code):** `SortHelper.Parse()` is fully tested but never called in production. The four repositories each independently implement sort parsing with their own dictionaries, creating a silent inconsistency — `SortHelper` throws on invalid fields while repos silently fall back to defaults.
- **Medium (Inconsistency):** `EmployeesController.Export` has `ext = "xls"` default; the other three controllers have no default, requiring `ext` to be explicitly provided.
- **Low (Error Handling):** `ArgumentException` from `SortHelper.Parse()` is not handled in `ExceptionHandlerMiddleware` (falls through to 500). Moot currently because repos don't call it, but latent risk if refactored to use the helper.

No critical security issues. Role scoping on exports is correctly implemented. Sort is whitelist-based and injection-safe.

**Overall Verdict: PASS WITH OBSERVATIONS** — no blocking issues; 4 Medium and several Low findings recommended for follow-up.

---

## 3. Findings Table

| ID    | Severity | Category       | File(s)                                                                                  | Finding                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       | Recommendation                                                                                                                                                                                                                                                                                                                                                                                                                             |
| ----- | -------- | -------------- | ---------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| CR-40 | Medium   | Performance    | All 4 repos `GetFilteredAllAsync`, `ExportService.cs`, All 4 controller export endpoints | **No max row limit on exports.** `GetFilteredAllAsync` returns ALL matching records with no upper bound. An unfiltered export could load tens of thousands of entities into memory, map them to DTOs, then serialize to a full in-memory byte array (XLSX via ClosedXML or PDF via QuestPDF). This is a potential OOM/DoS vector. The `ExportService` methods wrap synchronous ClosedXML/QuestPDF work in `Task.FromResult`, meaning the thread pool thread is blocked for the entire generation duration. For a 50k-row export, this could take several seconds and hold ~100 MB+ in memory. | Add a configurable max export row limit (e.g. 10,000). Enforce in the controller before calling `GetFilteredAllAsync`, or add a `LIMIT` to the repository query. Return 400 if the filtered count exceeds the limit. Also consider streaming XLSX output using `XLWorkbook.SaveAs(Response.Body)` to avoid full byte-array materialization, though QuestPDF does not support streaming.                                                    |
| CR-41 | Medium   | Dead Code      | `SortHelper.cs`, All 4 repos, `SortHelperTests.cs`                                       | **`SortHelper.Parse()` is never called in production.** The method exists, is tested (7 tests pass), but no controller or repo references it. Each repository independently implements sort parsing in its own `ApplySort` method with its own `SortFields` dictionary. The behavioral difference is significant: `SortHelper.Parse()` **throws `ArgumentException`** for invalid sort fields, while repo `ApplySort` methods **silently fall back to default sort**. All four repos have `using PAMS.Application.Helpers;` but never call `SortHelper`. The import is dead.                  | Either (a) use `SortHelper.Parse()` in the controllers to validate the sort parameter before passing it to the repo, and handle `ArgumentException` → 400 BadRequest, or (b) remove `SortHelper.Parse()` and its tests since the repos handle sort independently. Option (a) is preferred — it gives API consumers actionable error messages on invalid sort fields. If adopting (a), also add `ArgumentException` handling to middleware. |
| CR-42 | Medium   | Consistency    | `EmployeesController.cs` vs other 3 controllers                                          | **`ext` parameter default value inconsistency.** `EmployeesController.Export` declares `[FromQuery] string ext = "xls"`, providing a default. The other three controllers declare `[FromQuery] string ext` with no default. Calling `GET /employees/export` without `ext` defaults to XLS; calling `GET /allocations/export` without `ext` returns 400. The behavior difference across endpoints is surprising.                                                                                                                                                                               | Remove the default from `EmployeesController.Export` (`string ext` → no default) to be consistent with the other three export endpoints. All four should require `ext` explicitly.                                                                                                                                                                                                                                                         |
| CR-43 | Medium   | Performance    | `ProjectRepository.cs` (export path)                                                     | **`GetFilteredAllAsync` loads ALL allocations for ALL projects.** Same as prior finding CR-29, now extended to the export path. `.Include(p => p.Allocations)` on the export query materializes every allocation for every exported project, solely to compute `ResourceCount` in the controller. For an unfiltered export of 200 projects × 50 allocations each = 10,000 allocation rows hydrated and discarded.                                                                                                                                                                             | Same recommendation as CR-29: use SQL projection. For the export path, compute `ResourceCount` in the query via `.Select()` projection rather than eager-loading all allocations in memory.                                                                                                                                                                                                                                                |
| CR-44 | Low      | Error Handling | `ExceptionHandlerMiddleware.cs`, `SortHelper.cs`                                         | **`ArgumentException` not handled in middleware.** `SortHelper.Parse()` throws `ArgumentException` for invalid sort fields, but the exception handler middleware maps `ArgumentException` to the fallback `_` case → 500 Internal Server Error. Currently moot (repos don't call the helper), but becomes a bug if `SortHelper` is adopted per CR-41(a).                                                                                                                                                                                                                                      | Add an `ArgumentException` case to `ExceptionHandlerMiddleware`: `ArgumentException => (HttpStatusCode.BadRequest, "ERR_INVALID_ARGUMENT", "Bad Request")`.                                                                                                                                                                                                                                                                                |
| CR-45 | Low      | Code Quality   | `ExportService.cs`                                                                       | **`CancellationToken` parameter accepted but never used.** All four `Generate*Async` methods accept `CancellationToken ct` but never check it. ClosedXML and QuestPDF APIs are synchronous and don't support cancellation, so the token is a no-op. The `Async` suffix on the method names is misleading — these methods wrap synchronous work in `Task.FromResult`.                                                                                                                                                                                                                          | Add `ct.ThrowIfCancellationRequested()` as the first line in each method to support cancellation before starting generation. A comment documenting why true async isn't possible would clarify intent.                                                                                                                                                                                                                                     |
| CR-46 | Low      | Code Quality   | All 4 repos (`ApplySort` + `SortFields`)                                                 | **Sort field dictionaries duplicated across repos.** Each repo defines its own `static readonly Dictionary<string, Expression<Func<T, object>>> SortFields` with the same structure and parsing pattern (check `StartsWith('-')`, `TryGetValue`, fallback). The pattern is correct but boilerplate-heavy. Four independent whitelists need separate maintenance.                                                                                                                                                                                                                              | Consider extracting a shared `SortExtensions.ApplySort<T>()` helper that takes the query, sort string, default expression, and the sort-field dictionary. This eliminates the duplicated StartsWith/TryGetValue/fallback logic. Each repo provides only its dictionary.                                                                                                                                                                    |
| CR-47 | Low      | Robustness     | `ExportService.cs` (`GeneratePdf`)                                                       | **No pagination guidance for large PDF exports.** QuestPDF's `Table` within a `Page` grows across pages (footer shows page numbers), but extremely large datasets (10k+ rows with many columns) may produce very large PDFs with degraded rendering performance.                                                                                                                                                                                                                                                                                                                              | Document the practical export limit in API docs. For very large PDFs, consider a warning header or truncation notice. This is acceptable for current use (see CR-40 for the row limit recommendation).                                                                                                                                                                                                                                     |
| CR-48 | Low      | API Contract   | All 4 export endpoints                                                                   | **`ext` param name is non-standard.** REST APIs conventionally use `format` or content negotiation via `Accept` header. The parameter name `ext` (abbreviation for "extension") may confuse API consumers expecting `format=pdf`.                                                                                                                                                                                                                                                                                                                                                             | Acceptable as-is if documented. Consider renaming to `format` in a future API version for clarity. The current convention is internally consistent across all four endpoints.                                                                                                                                                                                                                                                              |
| CR-49 | Low      | Consistency    | All 4 repos (`ApplySort`, `BuildFilteredQuery`)                                          | **`DateTime.Today` vs `UtcNow` inconsistency persists (continuation of CR-30).** `AllocationRepository.BuildFilteredQuery` and `EmployeeRepository.BuildFilteredQuery` use `DateOnly.FromDateTime(DateTime.Today)` (local time). The export path also goes through these same methods, propagating timezone-dependent behavior to exported data.                                                                                                                                                                                                                                              | Standardize on `DateOnly.FromDateTime(DateTime.UtcNow)` across all repos and controllers, per prior recommendation CR-30.                                                                                                                                                                                                                                                                                                                  |
| CR-50 | Low      | Security       | `ExportService.cs` (`GenerateXlsx`)                                                      | **ClosedXML `AdjustToContents()` based on user data.** `worksheet.Columns().AdjustToContents()` processes all cell values. If user-controlled data (e.g. employee names, project names) contains extremely long strings, this could cause excessive processing time in the column width calculation.                                                                                                                                                                                                                                                                                          | Acceptable for current data shapes. Consider adding a max column width cap if user-input data could contain very long values.                                                                                                                                                                                                                                                                                                              |
| CR-51 | Info     | Architecture   | `IExportService.cs`, `ExportService.cs`, DI registration                                 | **Clean Architecture layering is correct.** `IExportService` interface + `ExportResult` record live in Application layer. `ExportService` implementation lives in Infrastructure layer. DI registration in `InfrastructureServiceExtensions` as Scoped. Dependency direction: Infrastructure → Application → Domain. No violations.                                                                                                                                                                                                                                                           | No action needed.                                                                                                                                                                                                                                                                                                                                                                                                                          |
| CR-52 | Info     | Architecture   | `Directory.Packages.props`, `PAMS.Infrastructure.csproj`                                 | **NuGet package management is correct.** Central Package Management (`ManagePackageVersionsCentrally = true`) used. ClosedXML 0.104.1 and QuestPDF 2024.12.3 versions are pinned centrally. `System.IO.Packaging 9.0.6` override addresses CVE in the transitive dependency with a clear comment. Infrastructure csproj references only package names (no versions) — correct CPM pattern.                                                                                                                                                                                                    | No action needed. Vulnerability override is well-documented.                                                                                                                                                                                                                                                                                                                                                                               |
| CR-53 | Info     | Quality        | `Program.cs`                                                                             | **QuestPDF license set correctly.** `QuestPDF.Settings.License = LicenseType.Community` placed before `WebApplication.CreateBuilder()`, ensuring the license is configured before any QuestPDF operation. Community license is free for revenue < $1M.                                                                                                                                                                                                                                                                                                                                        | No action needed. Ensure the revenue threshold is reviewed if the project is used commercially.                                                                                                                                                                                                                                                                                                                                            |
| CR-54 | Info     | Quality        | Export format handling (`xls` → `xlsx`)                                                  | **Format translation is correct.** Controllers accept `ext=xls` for user simplicity but pass `"xlsx"` to `ExportService`. `ExportService.Generate()` switch matches `"xlsx"` → `GenerateXlsx()`, `"pdf"` → `GeneratePdf()`, with `_ => throw ArgumentException`. Content type `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` is correct for `.xlsx`. Filename in response uses `.xlsx` (not `.xls`).                                                                                                                                                                     | No action needed.                                                                                                                                                                                                                                                                                                                                                                                                                          |
| CR-55 | Info     | Quality        | `SortHelperTests.cs`                                                                     | **SortHelper tests are well-structured.** 7 tests cover: null input, empty input, valid ascending, valid descending, case insensitivity, invalid field → ArgumentException, dash-only → ArgumentException. All edges of the `Parse` method are covered. However, these tests exercise code that is not called in production (see CR-41).                                                                                                                                                                                                                                                      | Tests are valuable if `SortHelper` is adopted per CR-41(a).                                                                                                                                                                                                                                                                                                                                                                                |
| CR-56 | Info     | Quality        | `SortEndpointTests.cs`, `ExportServiceTests.cs`                                          | **Structural/reflection tests verify interface contracts.** 8 tests confirm all 4 repository interfaces have `sort` param on `GetFilteredAsync` + `GetFilteredAllAsync` method. 6 tests confirm `IExportService` has all 4 Generate methods with correct signatures and `ExportResult` has expected properties. These are compile-guard tests — useful for TDD, less so post-implementation. **No behavioral tests** for `ExportService` (e.g. verify XLSX has correct headers, PDF is non-empty).                                                                                            | Consider adding behavioral tests for `ExportService` to verify generated output correctness.                                                                                                                                                                                                                                                                                                                                               |
| CR-57 | Info     | Security       | All 4 export endpoints                                                                   | **Export role scoping is correct.** Allocations export: inline scoping (Staff → own, PM → own projects, HR → all) matches the List endpoint. Projects/Accounts/Employees export: `[Authorize(Policy = "CanAllocate")]` restricts to HR + PM. Filenames are hardcoded (`"Allocations.xlsx"`, etc.) — no user input flows into filename construction. Download served via `File(bytes, contentType, fileName)` — safe.                                                                                                                                                                          | No action needed.                                                                                                                                                                                                                                                                                                                                                                                                                          |
| CR-58 | Info     | Security       | All 4 repos (`ApplySort`)                                                                | **Sort whitelist approach is injection-safe.** Each repo's `SortFields` dictionary maps user-provided field names to strongly typed `Expression<Func<T, object>>` selectors. The raw `sort` string never reaches EF Core's `OrderBy` — only pre-defined expressions are used. Unknown fields silently fall back to default sort. Correct pattern for preventing SQL injection via sort parameters.                                                                                                                                                                                            | No action needed.                                                                                                                                                                                                                                                                                                                                                                                                                          |

---

## 4. Detailed Analysis

### 4.1 Sort Implementation: Dual Parsing Paths (CR-41)

There are two independent sort-parsing paths in the codebase:

| Path                 | Location                 | Behavior on Invalid Field      | Used By                     |
| -------------------- | ------------------------ | ------------------------------ | --------------------------- |
| `SortHelper.Parse()` | Application layer        | **Throws `ArgumentException`** | Unit tests only (dead code) |
| Repo `ApplySort()`   | Each Infrastructure repo | **Silent fallback** to default | All controllers             |

The controllers pass the raw `sort` query param straight to the repository. The repo's `ApplySort` parses the `-` prefix, looks up the field in its own `SortFields` dictionary (case-insensitive), and falls back silently if not found.

```
Controller → repo.GetFilteredAsync(..., sort, ...) → repo.ApplySort(query, sort) → silent fallback
                                                          ↑ never calls SortHelper.Parse()
```

This means:

- Passing `sort=invalidField` returns results sorted by the default (no error) — potentially confusing for API consumers who don't realize their sort was ignored.
- `SortHelper` and its 7 tests are dead weight in the current architecture.

**If `SortHelper` is adopted:** Controllers would call `SortHelper.Parse()` first to validate. But `SortHelper.Parse()` returns `(PropertyName, IsDescending)` — the property name mapped from the whitelist — while repo `ApplySort` expects the raw sort string. The API surface doesn't align cleanly; a redesign would be needed to bridge them.

### 4.2 Export Memory Profile (CR-40)

Export request lifecycle and memory allocation:

```
1. Controller         → repo.GetFilteredAllAsync()  → loads ALL entities into memory
2. Controller         → .Select(MapFrom)            → creates full DTO list (second copy)
3. Controller         → exportService.Generate*()   → creates object?[] row list (third copy)
4. ExportService      → ClosedXML workbook / QuestPDF document (fourth copy in library internals)
5. ExportService      → ms.ToArray() / GeneratePdf() → final byte[] (fifth allocation)
6. Controller         → File(bytes, ...)            → written to response body
```

For N rows with M columns, approximate peak memory: `O(5 × N × M × avg_field_size)`. At 10,000 allocations × 11 columns × 50 bytes avg ≈ ~27 MB just for data, multiplied across copies ≈ **~135 MB peak** for a moderately sized export, all on a single request thread.

Recommended mitigations (in order of cost/effectiveness):

1. Max row limit (10,000) — cheapest, most effective
2. Streaming for XLSX (pipe `workbook.SaveAs()` to response stream) — avoids the final `ToArray()`
3. Background job for large exports (return 202 Accepted + poll URL) — most robust

### 4.3 Sort Field Coverage

Available sort fields per entity:

| Entity     | Sortable Fields                                                               | Default Sort                    |
| ---------- | ----------------------------------------------------------------------------- | ------------------------------- |
| Allocation | `fromDate`, `toDate`, `percentage`, `createdAt`                               | `fromDate` descending           |
| Project    | `projectName`, `projectCode`, `startDate`, `endDate`, `status`, `accountName` | `projectName` ascending         |
| Account    | `accountName`, `accountCode`, `accountType`                                   | `accountName` ascending         |
| Employee   | `firstName`, `lastName`, `empCode`, `designation`, `role`                     | `firstName` asc, `lastName` asc |

Observations:

- Allocation sort lacks `employeeName` and `projectName` — would require navigation-property-based expressions.
- `toDate` sort on allocations uses `a.ToDate!` (null-forgiving) — EF Core handles this safely by translating to SQL `ORDER BY to_date` with NULLs per PostgreSQL default behavior.
- `endDate` sort on projects also uses null-forgiving operator — same safe pattern.

### 4.4 Role Scoping Matrix (Exports)

| Endpoint                  | Auth Policy            | HR  | PM                             | Staff                |
| ------------------------- | ---------------------- | --- | ------------------------------ | -------------------- |
| `GET /allocations/export` | `[Authorize]` + inline | All | Own projects' allocations only | Own allocations only |
| `GET /projects/export`    | `CanAllocate`          | All | All (no PM scoping)            | ❌ Blocked by policy |
| `GET /accounts/export`    | `CanAllocate`          | All | All                            | ❌ Blocked by policy |
| `GET /employees/export`   | `CanAllocate`          | All | All                            | ❌ Blocked by policy |

Scoping is appropriate. Allocations export mirrors the List endpoint's inline scoping (CR-24 fix from prior review). Projects/Accounts/Employees exports are restricted to HR+PM via `CanAllocate`.

### 4.5 ExportService Design Review

| Aspect                   | Assessment                                                          |
| ------------------------ | ------------------------------------------------------------------- |
| Separation of concerns   | ✅ Interface in Application, implementation in Infrastructure       |
| DI lifetime              | ✅ Scoped — stateless service, no shared state                      |
| Format dispatch          | ✅ Clean switch expression with exhaustive matching                 |
| Header styling (XLSX)    | ✅ Bold headers, auto-adjusted column widths                        |
| PDF layout               | ✅ Landscape A4, font size 8, page numbers in footer                |
| Content types            | ✅ Correct XLSX and PDF MIME types                                  |
| Null handling            | ✅ `Blank.Value` for null XLSX cells, `""` for null PDF cells       |
| Date formatting          | ✅ `yyyy-MM-dd` ISO 8601 consistent with API convention             |
| Boolean/int type mapping | ✅ XLSX preserves native types (`int i`, `bool b`), not stringified |

---

## 5. Architecture Compliance

### 5.1 Dependency Direction

| Layer          | Allowed Dependencies             | New Dependencies in This Changeset                                      | Compliant? |
| -------------- | -------------------------------- | ----------------------------------------------------------------------- | ---------- |
| Domain         | None (BCL only)                  | Repo interfaces: only BCL types (`string?`, `int`, `CancellationToken`) | ✅         |
| Application    | Domain                           | `SortHelper` — BCL only; `IExportService` references Application DTOs   | ✅         |
| Infrastructure | Domain + Application + Libraries | `ExportService` → ClosedXML, QuestPDF; repos → EF Core                  | ✅         |
| API            | Application (composition root)   | Controllers → `IExportService` (Application interface via DI)           | ✅         |

`ExportResult` record is in the Application layer alongside `IExportService`. This is correct — it's a service result contract, not an infrastructure detail. The `byte[]` file content is a Value Object crossing the boundary; API layer writes it to the HTTP response.

### 5.2 Domain Interface Purity

The repository interfaces gained `string? sort` and `GetFilteredAllAsync`. These additions are appropriate:

- `sort` is a primitive string — no framework dependency leaked into Domain
- `GetFilteredAllAsync` follows the same pattern as `GetFilteredAsync` (minus pagination params)
- The sort semantics (field name, `-` prefix for descending) are a loose contract — the Domain interface doesn't dictate implementation

---

## 6. Edge Cases

| Edge Case                            | Handling                                                                     | Assessment                                                     |
| ------------------------------------ | ---------------------------------------------------------------------------- | -------------------------------------------------------------- |
| `sort=null` / `sort=""` / `sort=" "` | Repos: `string.IsNullOrWhiteSpace` guard → default sort                      | ✅ Correct                                                     |
| `sort=-` (dash only, no field)       | Repos: `TryGetValue("")` → not found → default sort (silent fallback)        | ✅ Safe, but `SortHelper` would throw                          |
| `sort=--fromDate` (double dash)      | `StartsWith('-')` strips first `-`, remaining `-fromDate` ≠ known → fallback | ✅ Safe                                                        |
| `sort=PROJECTNAME` (caps)            | `StringComparer.OrdinalIgnoreCase` → matches correctly                       | ✅ Correct                                                     |
| `ext=PDF` (uppercase)                | `ext?.ToLowerInvariant()` → `"pdf"` → matches                                | ✅ Correct                                                     |
| `ext=xlsx`                           | `format != "pdf" && format != "xls"` → BadRequest                            | ⚠️ Surprising — actual format is xlsx but users must say "xls" |
| `ext` omitted (Employees)            | Default `"xls"` → succeeds                                                   | ⚠️ Inconsistent with other endpoints (see CR-42)               |
| `ext` omitted (others)               | `ext` is null → `format` is null → BadRequest                                | ✅ Correct                                                     |
| Export with 0 results                | XLSX: empty worksheet with headers only; PDF: empty table with headers       | ✅ Correct                                                     |
| `toDate` sort with null values       | EF Core → SQL `ORDER BY to_date` (PostgreSQL: NULLs first/last default)      | ✅ Acceptable                                                  |

---

## 7. Test Coverage Assessment

### 7.1 What's Tested

| Test File               | Tests | Coverage Target                             | Coverage Quality             |
| ----------------------- | ----- | ------------------------------------------- | ---------------------------- |
| `SortHelperTests.cs`    | 7     | `SortHelper.Parse()` — all branches         | ✅ Excellent (but dead code) |
| `SortEndpointTests.cs`  | 8     | Repo interface contracts (reflection)       | ⚠️ Structural only           |
| `ExportServiceTests.cs` | 6     | `IExportService` + `ExportResult` contracts | ⚠️ Structural only           |

### 7.2 What's NOT Tested

| Gap                                                                      | Risk   |
| ------------------------------------------------------------------------ | ------ |
| `ExportService` actual output (XLSX content, PDF generation)             | Medium |
| Repo `ApplySort` behavior (sort order verification)                      | Medium |
| Controller export endpoint integration (format validation, role scoping) | Low    |
| Large export memory behavior                                             | Low    |

---

## 8. Verdict and Recommendations

### Verdict: **PASS WITH OBSERVATIONS**

Both features are correctly implemented with clean architecture, proper role scoping on exports, and injection-safe sort handling. Build is green with 231/231 tests passing. No critical or high-severity issues found.

**Four medium-severity findings warrant follow-up before production deployment with large datasets:**

1. **CR-40 (Medium):** No max export row limit — potential memory/DoS issue at scale.
2. **CR-41 (Medium):** `SortHelper.Parse()` is dead code — either adopt it for validation or remove it.
3. **CR-42 (Medium):** `ext` default inconsistency on EmployeesController.
4. **CR-43 (Medium):** ResourceCount loading loads all allocations in export path.

### Recommended Follow-Up Actions

| Priority   | Action                                                                                                 | Finding |
| ---------- | ------------------------------------------------------------------------------------------------------ | ------- |
| **Medium** | Add configurable max export row limit (e.g. 10,000); return 400 if exceeded                            | CR-40   |
| **Medium** | Either adopt `SortHelper.Parse()` in controllers + add `ArgumentException` → 400 mapping, or remove it | CR-41   |
| **Medium** | Remove `ext = "xls"` default from `EmployeesController.Export` for consistency                         | CR-42   |
| **Medium** | Use SQL projection for `ResourceCount` in export path (same as CR-29)                                  | CR-43   |
| Low        | Add `ArgumentException` case to `ExceptionHandlerMiddleware` (required if CR-41(a) adopted)            | CR-44   |
| Low        | Add `ct.ThrowIfCancellationRequested()` to `ExportService` methods                                     | CR-45   |
| Low        | Extract shared `ApplySort<T>()` helper to reduce boilerplate                                           | CR-46   |
| Low        | Document practical export size limits in API docs                                                      | CR-47   |
| Low        | Consider renaming `ext` to `format` in next API version                                                | CR-48   |
| Low        | Standardize on `DateTime.UtcNow` across all repos (carried from CR-30)                                 | CR-49   |
| Low        | Add behavioral tests for `ExportService` (verify XLSX headers, PDF non-empty)                          | CR-56   |

### Status of Prior Findings (from 2026-03-04 Review)

| Prior ID | Severity | Status      | Notes                                                                       |
| -------- | -------- | ----------- | --------------------------------------------------------------------------- |
| CR-24    | Critical | ✅ RESOLVED | Role scoping added to `GET /allocations` List + Export                      |
| CR-25    | High     | ⚠️ Open     | `UpdateAllocationCommandValidator` still missing                            |
| CR-26    | Medium   | ⚠️ Open     | Dead params `includeEnded`/`includeRemoved` still present                   |
| CR-27    | Medium   | ⚠️ Open     | Dead params `windowFrom`/`windowTo` still present                           |
| CR-28    | Medium   | ⚠️ Open     | Audit log still missing `Billable`/`ProjectRole`                            |
| CR-29    | Medium   | ⚠️ Open     | ResourceCount still loads all allocations (now also in export path — CR-43) |
| CR-30    | Low      | ⚠️ Open     | `DateTime.Today` vs `UtcNow` inconsistency persists (CR-49)                 |
| CR-31    | Low      | ⚠️ Open     | `ManagedProjectItem.cs` tombstone still exists                              |
| CR-32    | Low      | ⚠️ Open     | Include order in `AllocationRepository` unchanged                           |
| CR-33    | Low      | ⚠️ Open     | Redundant `DeletedAt == null` filter still present                          |

---

_End of Code Review Report — Sort Parameter + Export APIs Scope_
