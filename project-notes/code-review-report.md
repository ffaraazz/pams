# PAMS — Code Review Report

---

## 1. Document Control

| Field       | Value                                                                                                                                                                                                                                       |
| ----------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Project     | Project Allocation Management System (PAMS)                                                                                                                                                                                                 |
| Review Date | 2026-03-04                                                                                                                                                                                                                                  |
| Reviewer    | CodeReviewer (GitHub Copilot)                                                                                                                                                                                                               |
| Scope       | Billable field on Allocation, new `GET /allocations` list endpoint, ResourceCount on projects, EmployeeDetailResponse simplification (remove CurrentAllocations/ManagedProjects), AllocationDetailResponse refactoring to `MapFrom` factory |
| Verdict     | **PASS WITH OBSERVATIONS**                                                                                                                                                                                                                  |

### 1.1 Files Reviewed

| #   | File                                  | Change Summary                                                                                                                     |
| --- | ------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| 1   | `Allocation.cs` (Entity)              | Added `Billable` property with default `true`                                                                                      |
| 2   | `CreateAllocationCommand.cs`          | Added `Billable` init property with default `true`                                                                                 |
| 3   | `UpdateAllocationCommand.cs`          | Added `Billable?` (nullable for partial update)                                                                                    |
| 4   | `CreateAllocationCommandHandler.cs`   | Maps `Billable` from command to entity                                                                                             |
| 5   | `UpdateAllocationCommandHandler.cs`   | Conditional `Billable` update when `HasValue`                                                                                      |
| 6   | `AllocationDetailResponse.cs`         | Added `Billable` + `ProjectBillable`; refactored to static `MapFrom` factory                                                       |
| 7   | `EmployeeResponses.cs`                | Removed `CurrentAllocations` and `ManagedProjects` from `EmployeeDetailResponse`                                                   |
| 8   | `ManagedProjectItem.cs`               | Cleared — tombstone comment only                                                                                                   |
| 9   | `ProjectResponses.cs`                 | Added `ResourceCount` to both summary and detail                                                                                   |
| 10  | `AllocationsController.cs`            | New `GET /allocations` list endpoint; `Update` uses `UpdateAllocationRequest` body                                                 |
| 11  | `EmployeesController.cs`              | Simplified `BuildEmployeeDetailResponse` (no more embedded collections)                                                            |
| 12  | `ProjectsController.cs`               | ResourceCount computed from loaded Allocations                                                                                     |
| 13  | `IAllocationRepository.cs`            | Added `GetFilteredAsync` and `GetFilteredCountAsync`                                                                               |
| 14  | `AllocationRepository.cs`             | Implements filtered queries with `BuildFilteredQuery` helper                                                                       |
| 15  | `ProjectRepository.cs`                | `GetFilteredAsync` now includes `.Include(p => p.Allocations)` for ResourceCount; `GetByCodeAsync` includes `Allocations.Employee` |
| 16  | `CreateAllocationCommandValidator.cs` | Validates `fromDate >= DateTime.Today`                                                                                             |
| 17  | `AllocationConfiguration.cs`          | `.HasDefaultValue(true)` for Billable column                                                                                       |
| 18  | `AddBillableToAllocation.cs`          | Migration adds `billable boolean NOT NULL DEFAULT true`                                                                            |
| 19  | `DemoDataSeeder.cs`                   | All seed allocations set `Billable` explicitly                                                                                     |

---

## 2. Executive Summary

This feature set adds a `Billable` flag to allocations, introduces a filterable `GET /allocations` list endpoint, adds `ResourceCount` to project responses, and simplifies `EmployeeDetailResponse` by removing embedded collection properties (`CurrentAllocations`, `ManagedProjects`) in favor of the new dedicated allocation list endpoint. The `AllocationDetailResponse` was refactored from inline builds to a centralized `MapFrom` static factory — a direct fix for CR-12 from the prior review.

**The review identifies one critical security finding:** the new `GET /allocations` endpoint lacks role-scoping, allowing any authenticated user (including Staff) to query all allocations across the entire system. Per requirements, HR should see all, PMs should see their own projects' allocations, and Staff should see only their own allocations.

**One high-severity finding:** no `UpdateAllocationCommandValidator` exists, so update requests bypass percentage bounds/increment validation and the fromDate-not-in-the-past rule that the create path enforces.

Additionally, several medium-severity findings address dead parameters (`includeEnded`, `includeRemoved`, `windowFrom`, `windowTo`), missing audit fields, and a performance concern with loading all allocations just to compute `ResourceCount` on project list views.

**Overall Verdict: PASS WITH OBSERVATIONS** — 1 Critical + 1 High must be addressed before shipping.

---

## 3. Findings Table

| ID    | Severity | Category     | File(s)                                                                                                     | Finding                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       | Recommendation                                                                                                                                                                                                                                                                                                                                                                                                                                                                     |
| ----- | -------- | ------------ | ----------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| CR-24 | Critical | Security     | `AllocationsController.cs`                                                                                  | The new `GET /allocations` endpoint uses `[Authorize]` only — **no role-scoping policy**. Any authenticated user (including Staff) can query all allocations in the system with arbitrary filters (`empCode`, `projectCode`, `projectManagerEmpCode`). This allows a Staff user to enumerate every allocation, including other employees' percentages, project assignments, and billable status. The POST, PUT, PATCH, and DELETE actions correctly require `[Authorize(Policy = "CanAllocate")]`, making this an authorization asymmetry.                                                                                    | Add role-scoping logic: either (a) apply `[Authorize(Policy = "CanAllocate")]` and let HR/PM access freely, or (b) keep `[Authorize]` but add inline scoping — if the current user is Staff, auto-filter to `empCode = currentUser.EmpCode`; if PM, auto-filter to `projectManagerEmpCode = currentUser.EmpCode` (allowing the explicit filter to further narrow but not widen). Option (b) is preferred as it supports Staff viewing their own allocations via the same endpoint. |
| CR-25 | High     | Validation   | `UpdateAllocationCommand.cs`, Validators folder                                                             | No `UpdateAllocationCommandValidator` exists. The `CreateAllocationCommandValidator` enforces: percentage ≥ configured minimum, percentage ≤ 100, percentage is a multiple of the configured increment, `FromDate ≥ today`, and `ToDate ≥ FromDate`. **None of these rules apply to updates.** The `UpdateAllocationCommandHandler` uses `AllocationCapacityService.Validate()` for capacity, but that only checks total ≤ 100 — it does not validate percentage bounds, increment, or date rules. A PUT request with `Percentage: 0`, `Percentage: 3` (non-increment), or `FromDate: 2020-01-01` will bypass all validation. | Create `UpdateAllocationCommandValidator` applying the same percentage and date rules as the create validator. Note: the `fromDate >= today` rule may need adjustment for updates — an existing allocation might have a past `fromDate` that shouldn't be rejected. Consider: if `fromDate` is changing (differs from current), validate it's not in the past; if unchanged, allow it.                                                                                             |
| CR-26 | Medium   | Correctness  | `EmployeesController.cs`                                                                                    | `BuildEmployeeDetailResponse` accepts `includeEnded` and `includeRemoved` parameters but **never uses them**. The method always fetches all allocations via `_allocationRepo.GetByEmployeeAsync` (which returns all non-soft-deleted allocations due to global query filter) then filters to active-only for availability computation. The parameters are passed from `GetByCode` and `GetMe` but have zero effect on the response. API consumers sending `includeEnded=true` or `includeRemoved=true` get no behavioral difference.                                                                                          | Either (a) implement the filtering logic — use `includeEnded` to include/exclude ended allocations in a response collection, and use `includeRemoved` with `IgnoreQueryFilters()` for HR users — or (b) remove the parameters from the endpoint signature since the response no longer embeds allocations. Given the simplification that removed `CurrentAllocations` from the response, option (b) is cleaner.                                                                    |
| CR-27 | Medium   | Correctness  | `EmployeesController.cs`                                                                                    | `windowFrom` and `windowTo` query parameters are accepted by `GetByCode` and `GetMe` but **never passed** to any downstream computation. `BuildEmployeeDetailResponse` computes availability using `DateTime.Today` regardless of the window parameters. API documentation describes these as "Start/End of availability window" but they have no effect.                                                                                                                                                                                                                                                                     | Remove the unused parameters from the action signatures, or implement window-based availability: pass `windowFrom`/`windowTo` to the allocation filtering so availability is computed for a specific date range rather than always "today".                                                                                                                                                                                                                                        |
| CR-28 | Medium   | Audit        | `CreateAllocationCommandHandler.cs`, `UpdateAllocationCommandHandler.cs`                                    | The audit log for `allocation.created` includes `Percentage, FromDate, ToDate` but **not `Billable`**. The audit log for `allocation.updated` similarly omits `Billable`. Since `Billable` affects billing and revenue tracking, changes to this field should be auditable. Additionally, `ProjectRole` is also omitted from both audit logs.                                                                                                                                                                                                                                                                                 | Add `Billable` and `ProjectRole` to both audit log payloads. For update, also consider logging old vs. new values to support diff-based audit trails.                                                                                                                                                                                                                                                                                                                              |
| CR-29 | Medium   | Performance  | `ProjectRepository.cs`, `ProjectsController.cs`                                                             | `ProjectRepository.GetFilteredAsync` now includes `.Include(p => p.Allocations)` to support `ResourceCount` computation. This eagerly loads **all** allocations (including ended/upcoming) for every project in the paginated result. For a page of 10 projects with 50 allocations each, this loads 500 allocation rows just to count the active ones. The count is then computed in-memory with `.Count(a => a.DeletedAt == null && a.FromDate <= today && ...)`.                                                                                                                                                           | Use a SQL subquery or projection instead of loading full allocation entities. Options: (a) add a `ResourceCount` computed column or DB view, (b) use EF Core `.Select()` projection with a nested `.Count()` to push the filter to SQL, or (c) add a dedicated `GetResourceCountsByProjectIds(List<Guid> ids)` batch method that returns a dictionary. Option (b) is simplest: `.Select(p => new { Project = p, ResourceCount = p.Allocations.Count(a => ...) })`.                 |
| CR-30 | Low      | Consistency  | `AllocationRepository.cs`, `AllocationDetailResponse.cs`, `EmployeesController.cs`, `ProjectsController.cs` | `AllocationDetailResponse.ComputeAllocationStatus` uses `DateOnly.FromDateTime(DateTime.UtcNow)`. `AllocationRepository.BuildFilteredQuery` uses `DateOnly.FromDateTime(DateTime.Today)` (local time). `EmployeesController` and `ProjectsController` use `DateOnly.FromDateTime(DateTime.Today)`. If the server runs in a non-UTC timezone, "today" differs between the status computation and the filter query, causing inconsistent classification near midnight. **This was flagged in CR-15 (prior review) and persists in new code.**                                                                                   | Standardize on `DateOnly.FromDateTime(DateTime.UtcNow)` everywhere. The new `BuildFilteredQuery` method should use UTC.                                                                                                                                                                                                                                                                                                                                                            |
| CR-31 | Low      | Code Quality | `ManagedProjectItem.cs`                                                                                     | The file contains only a tombstone comment: `// Removed in v1.10.0 — ManagedProjects no longer embedded in EmployeeDetailResponse`. An empty/comment-only `.cs` file adds confusion — it compiles to nothing but appears in the project.                                                                                                                                                                                                                                                                                                                                                                                      | Delete the file entirely and remove any project reference. The removal is already documented in `api-flow.md`. If a placeholder is desired for version history, a note in the changelog or the API flow doc is sufficient.                                                                                                                                                                                                                                                         |
| CR-32 | Low      | Code Quality | `AllocationRepository.cs`                                                                                   | In `GetFilteredAsync`, the `.Include()` calls are placed **after** `.Skip().Take()`: `query.OrderBy(...).Skip(...).Take(...).Include(a => a.Employee).Include(...)`. While EF Core correctly rewrites the expression tree (Includes apply to the final query regardless of LINQ method order), this is unconventional and can mislead reviewers into thinking navigation properties won't be loaded for the paginated subset.                                                                                                                                                                                                 | Move `.Include()` calls before `.OrderBy().Skip().Take()` for readability and to match the conventional EF Core pattern.                                                                                                                                                                                                                                                                                                                                                           |
| CR-33 | Low      | Redundancy   | `AllocationRepository.cs`                                                                                   | `BuildFilteredQuery` starts with `.Where(a => a.DeletedAt == null)`, but `PamsDbContext` already applies `HasQueryFilter(a => a.DeletedAt == null)` globally. The explicit filter is redundant — EF will never return soft-deleted allocations unless `IgnoreQueryFilters()` is called. **This echoes CR-16 from the prior review.**                                                                                                                                                                                                                                                                                          | Remove the redundant check or add a comment: `// Defense-in-depth — global query filter already excludes soft-deleted`. The redundant filter generates an extra `AND deleted_at IS NULL` predicate in SQL, which is harmless but noisy in query plans.                                                                                                                                                                                                                             |
| CR-34 | Low      | Code Quality | `AllocationsController.cs`                                                                                  | `UpdateAllocationRequest` and `StopAllocationRequest` are defined as nested records at the bottom of the controller file. While functional, this pattern mixes API contract DTOs with controller routing logic. Other controllers (`ProjectsController`, `EmployeesController`) follow the same pattern, so it's consistent — but as the API grows, extracting request/response records into dedicated files improves discoverability.                                                                                                                                                                                        | Accept as-is for consistency with existing codebase patterns. Consider extracting to a `Requests/` folder in a future cleanup.                                                                                                                                                                                                                                                                                                                                                     |
| CR-35 | Low      | API Contract | `EmployeeResponses.cs`                                                                                      | `CurrentAllocations` and `ManagedProjects` removed from `EmployeeDetailResponse`. This is a **breaking change** for API consumers that rely on these fields. Existing clients reading `currentAllocations[]` from the JSON response will now receive `undefined`/missing. The `api-flow.md` documents this as a v1.10.0 removal with migration guidance (`GET /allocations?empCode=...` instead).                                                                                                                                                                                                                             | The breaking change is intentional and documented. Ensure the API version is bumped if semantic versioning is used. Add a note to the OpenAPI spec changelog. The unit tests (`EmployeeDetailResponseSimplificationTests`) correctly verify the properties are absent.                                                                                                                                                                                                             |
| CR-36 | Info     | Quality      | `AllocationDetailResponse.cs`                                                                               | The refactoring from inline builds to `MapFrom(Allocation, Employee?, Project?)` factory is a direct fix for **CR-12** from the prior review. All controller inline builds are now eliminated — `GetById`, `Stop`, `Remove`, and `GetFilteredAsync` all use `MapFrom`. The `MapFrom(Allocation)` overload accesses navigation properties, requiring callers to ensure eager loading. This is documented in the XML comment.                                                                                                                                                                                                   | Good improvement. No action needed.                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| CR-37 | Info     | Quality      | `AllocationDetailResponse.cs`                                                                               | `ProjectBillable` is a new property that surfaces `project.Billable` alongside the allocation-level `Billable`. This enables UI to distinguish "this allocation is billable" from "this project is billable" — useful when a non-billable allocation exists on a billable project (e.g., internal QA on a client project). Good design decision.                                                                                                                                                                                                                                                                              | No action needed.                                                                                                                                                                                                                                                                                                                                                                                                                                                                  |
| CR-38 | Info     | Migration    | `AddBillableToAllocation.cs`                                                                                | Migration adds `billable boolean NOT NULL DEFAULT true`. Existing rows will get `true` as the default. The `Down` migration correctly drops the column. The `AllocationConfiguration.cs` matches with `.HasDefaultValue(true)`. The migration timestamp `20260304000000` follows the project convention. **Migration is safe and correct.**                                                                                                                                                                                                                                                                                   | No action needed.                                                                                                                                                                                                                                                                                                                                                                                                                                                                  |
| CR-39 | Info     | Seed Data    | `DemoDataSeeder.cs`                                                                                         | All 12 allocation seed records now explicitly set `Billable`. This includes `Billable = false` for the AI/ML POC allocation (non-billable project) and the soft-deleted E-Commerce allocation — demonstrating both true and false values. The seed data is realistic and exercises the new field.                                                                                                                                                                                                                                                                                                                             | No action needed.                                                                                                                                                                                                                                                                                                                                                                                                                                                                  |

---

## 4. Detailed Analysis

### 4.1 Security: GET /allocations Role-Scoping (CR-24)

The new endpoint's authorization compared to sibling actions:

| Action          | HTTP Method | Policy                            | Role Scope     |
| --------------- | ----------- | --------------------------------- | -------------- |
| `List`          | GET         | `[Authorize]` (any authenticated) | **None — gap** |
| `Create`        | POST        | `CanAllocate`                     | HR + PM        |
| `GetById`       | GET         | `[Authorize]`                     | Any            |
| `Update`        | PUT         | `CanAllocate`                     | HR + PM        |
| `Stop`          | PATCH       | `CanAllocate`                     | HR + PM        |
| `Remove`        | DELETE      | `CanAllocate`                     | HR + PM        |
| `CapacityCheck` | GET         | `CanAllocate`                     | HR + PM        |

The `GetById` endpoint also has `[Authorize]` only, but it requires knowing a specific allocation GUID — it's not enumerable. The `List` endpoint is freely enumerable with no filter constraints.

**Proposed scoping logic for List:**

```csharp
// Inside List action, before calling repository:
if (_currentUser.Role == EmployeeRole.Staff)
{
    empCode = _currentUser.EmpCode; // Force to own allocations only
}
else if (_currentUser.Role == EmployeeRole.ProjectManager)
{
    projectManagerEmpCode ??= _currentUser.EmpCode; // Default to own, allow narrowing
}
// HR: no restriction
```

### 4.2 Validation Gap: UpdateAllocationCommand (CR-25)

Rules comparison between Create and Update paths:

| Validation Rule                 | Create (via Validator) | Update (no Validator)                       | Gap?                                            |
| ------------------------------- | ---------------------- | ------------------------------------------- | ----------------------------------------------- |
| Percentage ≥ configured minimum | ✅ FluentValidation    | ❌ Not checked                              | Yes                                             |
| Percentage ≤ 100                | ✅ FluentValidation    | ⚠️ CapacityService total                    | Partial — capacity checks total, not individual |
| Percentage % increment == 0     | ✅ FluentValidation    | ❌ Not checked                              | Yes                                             |
| FromDate ≥ today                | ✅ FluentValidation    | ❌ Not checked                              | Yes                                             |
| ToDate ≥ FromDate               | ✅ FluentValidation    | ❌ Not checked (DB check constraint exists) | DB-level only                                   |
| Total allocation ≤ 100%         | ✅ CapacityService     | ✅ CapacityService                          | No                                              |

The DB check constraint `chk_allocation_percentage` (`percentage BETWEEN 1 AND 100`) will catch extreme values at the persistence layer, but it won't enforce the increment rule or the from-date rule. Users will receive a raw DB exception (500) instead of a validation error (400).

### 4.3 Performance: ResourceCount Loading (CR-29)

Query profile for `GET /projects` list:

```
Current:
  SELECT p.*, a.* FROM projects p
  LEFT JOIN allocations a ON a.project_id = p.id AND a.deleted_at IS NULL
  ORDER BY p.project_name
  OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY

Proposed (projection):
  SELECT p.*,
    (SELECT COUNT(*) FROM allocations a
     WHERE a.project_id = p.id AND a.deleted_at IS NULL
     AND a.from_date <= @today AND (a.to_date IS NULL OR a.to_date >= @today)) AS ResourceCount
  FROM projects p
  ORDER BY p.project_name
  OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY
```

The current approach materializes all allocation rows into memory. For a project with 100 historical allocations, only ~5 may be active. The remaining 95 rows are loaded, hydrated into C# objects, and immediately discarded by the `.Count()` filter.

### 4.4 Breaking Changes Assessment

| Removed Property                            | Previously Used By       | Replacement                                                                                     | Breaking?        |
| ------------------------------------------- | ------------------------ | ----------------------------------------------------------------------------------------------- | ---------------- |
| `EmployeeDetailResponse.CurrentAllocations` | Frontend / API consumers | `GET /allocations?empCode={code}`                                                               | Yes (documented) |
| `EmployeeDetailResponse.ManagedProjects`    | Frontend / API consumers | `GET /allocations?projectManagerEmpCode={code}` or `GET /projects?projectManagerEmpCode={code}` | Yes (documented) |

The removals are documented in `api-flow.md` as v1.10.0 changes with clear migration paths. Unit tests (`EmployeeDetailResponseSimplificationTests`) verify the properties are absent via reflection. **The breaking changes are intentional and handled correctly.**

### 4.5 Dead Parameters (CR-26, CR-27)

The `EmployeesController.GetByCode` action signature:

```csharp
public async Task<ActionResult<EmployeeDetailResponse>> GetByCode(
    string empCode,
    [FromQuery] DateOnly? windowFrom = null,    // ← never used
    [FromQuery] DateOnly? windowTo = null,      // ← never used
    [FromQuery] bool includeEnded = false,       // ← accepted but ignored
    [FromQuery] bool includeRemoved = false,     // ← accepted but ignored
    CancellationToken ct = default)
```

`BuildEmployeeDetailResponse` signature and body:

```csharp
private async Task<EmployeeDetailResponse> BuildEmployeeDetailResponse(
    Employee employee, bool includeEnded, bool includeRemoved, CancellationToken ct)
{
    // includeEnded and includeRemoved are never referenced in the method body
    var allocations = await _allocationRepo.GetByEmployeeAsync(employee.Id, ct);
    // ... computes availability from active allocations only
}
```

Since `CurrentAllocations` was removed from the response, these parameters lost their purpose. They should be removed to avoid misleading API consumers.

### 4.6 Migration Safety (CR-38)

| Aspect              | Value                                | Assessment                                        |
| ------------------- | ------------------------------------ | ------------------------------------------------- |
| Column              | `billable`                           | ✅ Follows snake_case convention                  |
| Type                | `boolean`                            | ✅ Correct for PostgreSQL                         |
| Nullable            | `NOT NULL`                           | ✅ Appropriate — billable has a definite answer   |
| Default             | `true`                               | ✅ Safe for existing rows; matches entity default |
| Down migration      | `DropColumn`                         | ✅ Clean rollback                                 |
| Schema              | `pams`                               | ✅ Matches existing schema                        |
| EF Config alignment | `.HasDefaultValue(true)`             | ✅ Matches migration                              |
| Entity alignment    | `bool Billable { get; set; } = true` | ✅ Matches both config and migration              |

**Migration is safe for production deployment.** Existing allocations will default to `billable = true`, which is the correct business default (allocations are billable unless explicitly marked otherwise).

---

## 5. Architecture Compliance

### 5.1 Dependency Direction

| Layer          | Allowed Dependencies           | Actual Dependencies in Changed Files                                      | Compliant? |
| -------------- | ------------------------------ | ------------------------------------------------------------------------- | ---------- |
| Domain         | None (BCL only)                | `Allocation.cs` — BCL types only (`Guid`, `DateOnly`, `bool`, `DateTime`) | ✅         |
| Application    | Domain                         | Commands reference Domain entities/repos; DTOs reference Domain entities  | ✅         |
| Infrastructure | Domain + Application           | Repository implements `IAllocationRepository` from Domain; uses EF Core   | ✅         |
| API            | Application (composition root) | Controllers use Application DTOs + Domain repositories (via DI)           | ✅         |

### 5.2 MapFrom Factory Pattern

The refactoring of `AllocationDetailResponse` from inline builds to `MapFrom` resolves CR-12 from the prior review. The two overloads provide flexibility:

- `MapFrom(Allocation)` — requires navigation properties loaded (used after Include-heavy queries)
- `MapFrom(Allocation, Employee?, Project?)` — accepts separately-fetched entities (used after individual lookups)

This is a clean, idiomatic mapping pattern. The nullable parameters on the second overload (`Employee?`, `Project?`) handle scenarios where related entities might not be found, defaulting to `string.Empty` via null-conditional operators.

---

## 6. Edge Cases

| Edge Case                                     | Handling                                                                                                                                 | Assessment                                          |
| --------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------- |
| ResourceCount when project has no allocations | `(p.Allocations ?? []).Count(...)` returns 0                                                                                             | ✅ Correct                                          |
| Status filter with null ToDate                | `BuildFilteredQuery` handles: `"active"` → `a.ToDate == null \|\| a.ToDate >= today`; `"ended"` → `a.ToDate != null && a.ToDate < today` | ✅ Correct                                          |
| Status filter with invalid value              | `_ => query` — returns unfiltered (ignores unknown status)                                                                               | ⚠️ Consider returning 400 for unknown status values |
| Billable filter with no value                 | `if (billable.HasValue)` guard — no filter applied                                                                                       | ✅ Correct                                          |
| Update with `Billable = null`                 | `if (request.Billable.HasValue)` — no change to existing value                                                                           | ✅ Correct                                          |
| Update with `ProjectRole = null`              | `if (request.ProjectRole is not null)` — no change                                                                                       | ✅ Correct, but prevents clearing ProjectRole       |

**Note on ProjectRole clearing:** The `if (request.ProjectRole is not null)` guard means there's no way to clear a ProjectRole back to null once set. A client sending `"projectRole": null` in JSON will result in `request.ProjectRole` being null, which skips the update. If clearing is a valid use case, consider using a sentinel value or a separate nullable wrapper.

---

## 7. Verdict and Recommendations

### Verdict: **PASS WITH OBSERVATIONS**

The Billable feature is well-implemented across all layers (entity → command → handler → DTO → migration → configuration → seed). The `AllocationDetailResponse.MapFrom` refactoring is a quality improvement. The EmployeeDetailResponse simplification is clean and well-documented with proper migration guidance.

**Two findings must be addressed before shipping:**

1. **CR-24 (Critical):** `GET /allocations` has no role-scoping — Staff users can enumerate all allocations system-wide.
2. **CR-25 (High):** Missing `UpdateAllocationCommandValidator` allows bypassing percentage bounds, increment, and date validation rules on updates.

### Recommended Follow-Up Actions

| Priority     | Action                                                                                               | Finding |
| ------------ | ---------------------------------------------------------------------------------------------------- | ------- |
| **Critical** | Add role-scoping to `GET /allocations` — force Staff to own allocations, PM to own projects          | CR-24   |
| **High**     | Create `UpdateAllocationCommandValidator` with percentage bounds/increment and date rules            | CR-25   |
| Medium       | Remove dead `includeEnded`, `includeRemoved` parameters from `GetByCode`/`GetMe` (or implement them) | CR-26   |
| Medium       | Remove dead `windowFrom`, `windowTo` parameters from `GetByCode`/`GetMe` (or implement them)         | CR-27   |
| Medium       | Add `Billable` and `ProjectRole` to audit log payloads in both command handlers                      | CR-28   |
| Medium       | Use SQL projection for ResourceCount instead of loading all allocation entities                      | CR-29   |
| Low          | Standardize on `DateTime.UtcNow` in `BuildFilteredQuery` and all controllers (unresolved from CR-15) | CR-30   |
| Low          | Delete `ManagedProjectItem.cs` tombstone file                                                        | CR-31   |
| Low          | Move `Include()` calls before `Skip().Take()` in `AllocationRepository.GetFilteredAsync`             | CR-32   |
| Low          | Remove redundant `DeletedAt == null` filter in `BuildFilteredQuery` (global query filter handles it) | CR-33   |
| Low          | Consider returning 400 for unknown status filter values instead of silently ignoring                 | —       |

---

_End of Code Review Report — Billable + Allocation List + Simplification Scope_
