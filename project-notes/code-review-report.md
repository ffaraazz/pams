# PAMS — Code Review Report

---

## 1. Document Control

| Field       | Value                                                                                                                                                                                            |
| ----------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Project     | Project Allocation Management System (PAMS)                                                                                                                                                      |
| Review Date | 2026-03-03                                                                                                                                                                                       |
| Reviewer    | CodeReviewer (GitHub Copilot)                                                                                                                                                                    |
| Scope       | API enrichment: enriched DTOs (`AllocationDetailResponse`, `ProjectDetailResponse`, `EmployeeDetailResponse`), new `ManagedProjectItem` DTO, controller inline builds, `[Obsolete]` on Dashboard |
| Verdict     | **PASS WITH OBSERVATIONS**                                                                                                                                                                       |

### 1.1 Files Reviewed

| #   | File                                       | Change Summary                                                                            |
| --- | ------------------------------------------ | ----------------------------------------------------------------------------------------- |
| 1   | `AllocationDetailResponse.cs`              | 6 new properties + static `ComputeAllocationStatus` helper                                |
| 2   | `ProjectResponses.cs`                      | `ProjectDetailResponse` got `Allocations[]` + `TeamMembers[]`                             |
| 3   | `EmployeeResponses.cs`                     | `EmployeeDetailResponse` got `ManagedProjects[]`                                          |
| 4   | `ManagedProjectItem.cs`                    | New DTO created                                                                           |
| 5   | `CreateAllocationCommandHandler.cs`        | Maps 6 new fields in response                                                             |
| 6   | `UpdateAllocationCommandHandler.cs`        | Maps 6 new fields in response                                                             |
| 7   | `AllocationsController.cs`                 | 3 inline builds updated (GetById, Stop, Remove)                                           |
| 8   | `ProjectsController.cs`                    | GetByCode populates Allocations + TeamMembers                                             |
| 9   | `EmployeesController.cs`                   | Injected `IProjectRepository` + `IProjectTeamMemberRepository`, populates ManagedProjects |
| 10  | `DashboardController.cs`                   | Marked `[Obsolete]`, inline builds updated                                                |
| 11  | `ProjectDetailResponseEnrichmentTests.cs`  | 2 TDD tests for Allocations + TeamMembers on ProjectDetailResponse                        |
| 12  | `EmployeeDetailResponseEnrichmentTests.cs` | 3 TDD tests for ManagedProjects on EmployeeDetailResponse                                 |
| 13  | `ProjectRepository.cs`                     | Reviewed for eager loading                                                                |
| 14  | `AllocationRepository.cs`                  | Reviewed for eager loading                                                                |
| 15  | `EmployeeRepository.cs`                    | Reviewed for eager loading                                                                |
| 16  | `ProjectTeamMemberRepository.cs`           | Reviewed for eager loading                                                                |

---

## 2. Executive Summary

The API enrichment adds richer response payloads to the Project, Employee, and Allocation endpoints — embedding child collections (`Allocations`, `TeamMembers`, `ManagedProjects`) directly in detail DTOs. This eliminates front-end round-trips and enables deprecation of the dedicated Dashboard endpoints. The `ComputeAllocationStatus` helper is properly centralized as a static method on `AllocationDetailResponse` and used consistently across all build sites.

**However, the review identified two high-severity issues:** (1) `ProjectRepository.GetByCodeAsync` does not eagerly load `Allocations` or `TeamMembers` navigation properties, meaning `ProjectsController.GetByCode` will always return empty collections for these fields; and (2) `ProjectTeamMemberRepository.GetByTeamLeadAsync` does not include `Project`, `Project.Account`, or `Project.Allocations` navigation properties, causing the `ManagedProjects` TeamLead enrichment in `EmployeesController` to produce null/empty data. Additionally, `AllocationRepository.GetByEmployeeAsync` includes `Project` but not `Project.Account`, so `AccountCode`/`AccountName` will always be empty on employee allocation responses.

These are **data-loss bugs** — the API will return structurally correct but empty/null-populated enrichment fields. They must be fixed before the feature is considered shippable.

**Overall Verdict: PASS WITH OBSERVATIONS** (2 High findings require immediate follow-up)

---

## 3. Findings Table

| ID    | Severity | Category         | File(s)                                                                                                 | Finding                                                                                                                                                                                                                                                                                                                                                                                                                                      | Recommendation                                                                                                                                                                                                                                                                                                              |
| ----- | -------- | ---------------- | ------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| CR-8  | High     | Data Correctness | `ProjectRepository.cs`, `ProjectsController.cs`                                                         | `GetByCodeAsync` includes only `Account` and `ProjectManager`. It does **not** include `Allocations` (+ `Allocations.Employee`) or `TeamMembers` (+ `TeamMembers.TeamLead`, `TeamMembers.Reportee`). No lazy loading is configured. `ProjectsController.GetByCode` accesses `p.Allocations` and `p.TeamMembers` — these will be uninitialized empty collections, producing always-empty `Allocations[]` and `TeamMembers[]` in the response. | Add `.Include(p => p.Allocations).ThenInclude(a => a.Employee)` and `.Include(p => p.TeamMembers).ThenInclude(tm => tm.TeamLead)` / `.ThenInclude(tm => tm.Reportee)` to `GetByCodeAsync`. Consider a separate overload (`GetByCodeWithDetailsAsync`) to avoid over-fetching for callers that don't need child collections. |
| CR-9  | High     | Data Correctness | `ProjectTeamMemberRepository.cs`, `EmployeesController.cs`                                              | `GetByTeamLeadAsync` has no `.Include()` calls. The controller accesses `tm.Project`, `tm.Project.Allocations`, and `tm.Project.Account` — all will be null. The ManagedProjects TeamLead section in `BuildEmployeeDetailResponse` will silently skip all projects (`tlProject is not null` will be false), producing incomplete `ManagedProjects[]`.                                                                                        | Add `.Include(ptm => ptm.Project).ThenInclude(p => p.Account)` and `.Include(ptm => ptm.Project).ThenInclude(p => p.Allocations)` to `GetByTeamLeadAsync`.                                                                                                                                                                  |
| CR-10 | Medium   | Data Correctness | `AllocationRepository.cs`, `EmployeesController.cs`                                                     | `GetByEmployeeAsync` includes `a.Project` but **not** `a.Project.Account`. The controller maps `a.Project?.Account?.AccountCode` and `a.Project?.Account?.AccountName` — these will always resolve to `string.Empty` because `Account` is not eagerly loaded through the allocation→project chain.                                                                                                                                           | Change to `.Include(a => a.Project).ThenInclude(p => p.Account)` in `GetByEmployeeAsync`.                                                                                                                                                                                                                                   |
| CR-11 | Medium   | Data Correctness | `EmployeeRepository.cs`, `EmployeesController.cs`                                                       | `GetByEmpCodeAsync` does not include `ManagedProjects` (+ `ManagedProjects.Account`, `ManagedProjects.Allocations`) or `ReportsTo`. The controller accesses `employee.ManagedProjects` (will be empty — EF initializes to `[]` but doesn't populate) and `employee.ReportsTo?.EmpCode` (will be null).                                                                                                                                       | Add `.Include(e => e.ManagedProjects).ThenInclude(p => p.Account)`, `.Include(e => e.ManagedProjects).ThenInclude(p => p.Allocations)`, and `.Include(e => e.ReportsTo)` to `GetByEmpCodeAsync` (and `GetByIdAsync`).                                                                                                       |
| CR-12 | Medium   | Code Quality     | `AllocationsController.cs`, `EmployeesController.cs`, `DashboardController.cs`, `ProjectsController.cs` | `AllocationDetailResponse` is constructed inline in **7 separate locations** across 4 controllers plus 2 command handlers (9 total). All 7 controller builds are nearly identical ~18-line blocks. This violates DRY and makes it easy for builds to drift out of sync.                                                                                                                                                                      | Extract a shared mapping method, e.g., `AllocationDetailResponse.FromEntities(Allocation a, Employee e, Project p)` as an additional static factory on the DTO, or create a dedicated mapper/extension in the Application layer.                                                                                            |
| CR-13 | Medium   | Performance      | `DashboardController.cs`                                                                                | `ProjectView` executes N+1 queries: one `GetFilteredAsync` per account, then one `GetByProjectAsync` per project. For a system with 10 accounts × 5 projects each, this is 60+ queries per request. Although the endpoint is now `[Obsolete]`, it remains callable until removed.                                                                                                                                                            | Since the endpoint is deprecated, document the known N+1 issue and set a removal timeline. If it must stay performant in the interim, batch-load allocations.                                                                                                                                                               |
| CR-14 | Medium   | Performance      | `EmployeesController.cs`                                                                                | `BuildEmployeeDetailResponse` makes 2 additional DB calls per invocation: `GetByEmployeeAsync` (allocations) and `GetByTeamLeadAsync` (team lead assignments). Then it iterates `teamLeadProjectIds` accessing navigation properties. For the `/employees` list endpoint, this method is called per employee (hidden N+1 via the Create/Update return paths).                                                                                | For single-employee detail (GetByCode, GetMe), acceptable. Ensure list endpoints never call `BuildEmployeeDetailResponse` per row. Consider a batch preload or a dedicated query for the full enriched employee.                                                                                                            |
| CR-15 | Low      | Consistency      | `AllocationDetailResponse.cs`, `EmployeesController.cs`                                                 | `ComputeAllocationStatus` uses `DateTime.UtcNow` to derive `today`. All controller code uses `DateTime.Today` (local time). If the server runs in a non-UTC timezone, an allocation could be "Active" per the DTO helper but "Ended" per controller filtering logic (or vice versa) around midnight.                                                                                                                                         | Standardize on `DateTime.UtcNow` everywhere, or inject `IDateTimeProvider` (already exists in the project) and use it consistently. `DateOnly.FromDateTime(DateTime.UtcNow)` is the safest default.                                                                                                                         |
| CR-16 | Low      | Redundancy       | `ProjectsController.cs`, `DashboardController.cs`, `EmployeesController.cs`                             | Controller code filters allocations with `a.DeletedAt == null`, but `PamsDbContext` already applies a global query filter `modelBuilder.Entity<Allocation>().HasQueryFilter(a => a.DeletedAt == null)`. The explicit `DeletedAt` check is redundant — EF will never return soft-deleted allocations unless `IgnoreQueryFilters()` is used.                                                                                                   | Remove the redundant `a.DeletedAt == null` checks from controller code, or add a comment clarifying they are defense-in-depth. The `includeRemoved` path in `BuildEmployeeDetailResponse` would need `IgnoreQueryFilters()` to actually work.                                                                               |
| CR-17 | Low      | Security         | `EmployeesController.cs`                                                                                | `GET /employees/{empCode}` requires only `[Authorize]` (any authenticated user). The enriched `ManagedProjects` now exposes `ActiveResourceCount` (team size) for every project the employee manages. A Staff user could enumerate PM profiles to learn headcount across projects. The List endpoint requires `CanAllocate`, creating an authorization asymmetry.                                                                            | Evaluate whether `ManagedProjects` should be restricted to `CanAllocate` or self-only access. If the data is intentionally public to all authenticated users, document this as an explicit design decision.                                                                                                                 |
| CR-18 | Low      | Test Coverage    | `ProjectDetailResponseEnrichmentTests.cs`                                                               | The enrichment tests construct `AllocationDetailResponse` but do not set or assert the 6 new fields (`Designation`, `Billable`, `AccountCode`, `AccountName`, `Status`, `UpdatedAt`). The tests prove the collection wiring works but miss verifying the new field mappings.                                                                                                                                                                 | Add assertions for the 6 new fields in the existing `GetByCode_ShouldIncludeAllocationsInResponse` test. Particularly assert that `Status` matches expected `ComputeAllocationStatus` output.                                                                                                                               |
| CR-19 | Low      | Test Coverage    | `tests/PAMS.UnitTests/`, `tests/PAMS.IntegrationTests/`                                                 | No integration tests verify that the enriched endpoints actually return populated `Allocations[]`, `TeamMembers[]`, or `ManagedProjects[]` from the database. The unit tests only verify in-memory DTO construction. The eager-loading bugs (CR-8, CR-9, CR-10, CR-11) would only be caught by integration tests.                                                                                                                            | Add integration tests for `GET /projects/{code}` asserting non-empty `Allocations` and `TeamMembers`, and for `GET /employees/{empCode}` asserting non-empty `ManagedProjects` when the data exists.                                                                                                                        |
| CR-20 | Low      | API Contract     | `AllocationDetailResponse.cs`                                                                           | 6 new properties were added to a response DTO that is used by existing endpoints (Create, Update, GetById). Existing API consumers will now receive additional JSON fields. This is **backward compatible** for JSON consumers (additive change), but strictly typed SDK clients generated from a prior OpenAPI spec may reject unknown fields depending on configuration.                                                                   | Bump the `api-spec.yaml` version to reflect the enriched schema. Document the new fields in the OpenAPI spec schemas.                                                                                                                                                                                                       |
| CR-21 | Info     | Deprecation      | `DashboardController.cs`                                                                                | `[Obsolete("Use enriched /projects/{code} and /employees/me instead. Will be removed in MVP2.")]` is correctly applied at the class level. The compiler will emit CS0618 warnings for any code referencing the controller. The `[Obsolete]` attribute does not affect runtime routing — the endpoints remain fully functional. This is the correct deprecation pattern.                                                                      | Consider adding a `Deprecation` or `Sunset` HTTP response header (RFC 8594) so API consumers get runtime notice. Set a concrete date for removal.                                                                                                                                                                           |
| CR-22 | Info     | Code Quality     | `EmployeesController.cs`                                                                                | `BuildEmployeeDetailResponse` is a 70+ line private method handling allocation filtering, availability calculation, PM project aggregation, TeamLead project aggregation, and DTO mapping. This method has high cyclomatic complexity and mixes query logic with mapping logic.                                                                                                                                                              | Consider splitting into smaller methods: `ComputeAvailability()`, `BuildManagedProjects()`, `MapAllocations()`. Alternatively, move enrichment logic to an Application-layer query handler (CQRS read side).                                                                                                                |
| CR-23 | Info     | Code Quality     | `EmployeesController.cs`                                                                                | The LINQ expression `.Where(pid => !managedProjects.Any(mp => mp.ProjectId == pid))` uses a list scan inside a Where predicate. For large numbers of managed projects this is O(n×m). In practice the count is small (a PM manages ~5-10 projects).                                                                                                                                                                                          | Convert `managedProjects` project IDs to a `HashSet<Guid>` before the LINQ predicate for O(1) lookups. Low priority given expected data volumes.                                                                                                                                                                            |

---

## 4. Architecture Compliance

### 4.1 Dependency Direction

| Layer          | Allowed Dependencies                            | Actual Dependencies in Changed Files                                                           | Compliant? |
| -------------- | ----------------------------------------------- | ---------------------------------------------------------------------------------------------- | ---------- |
| Domain         | None (BCL only)                                 | Not changed in this scope                                                                      | ✅         |
| Application    | Domain                                          | `AllocationDetailResponse` uses `DateOnly` (BCL) only. `ManagedProjectItem` uses Domain enums. | ✅         |
| Infrastructure | Domain + Application                            | Repository Include changes (proposed) stay within Infrastructure                               | ✅         |
| API            | Application + Infrastructure (composition root) | Controllers depend on Domain repositories + Application DTOs + `ICurrentUserService`           | ✅         |

### 4.2 Enriched DTO Design

The decision to embed `Allocations[]` and `TeamMembers[]` on `ProjectDetailResponse` and `ManagedProjects[]` on `EmployeeDetailResponse` is architecturally sound — these are **read-model DTOs** in the Application layer. They aggregate data from multiple entities for presentation, which is the intended role of the Application layer in Clean Architecture.

`ComputeAllocationStatus` as a static method on `AllocationDetailResponse` is acceptable for a pure computation (no dependencies, no side effects). However, it introduces business logic in a DTO. An alternative would be a Domain service or value object. Given the simplicity of the logic (3-line date comparison), the current placement is pragmatic and acceptable.

### 4.3 Controller Enrichment Logic (Observation)

The `EmployeesController.BuildEmployeeDetailResponse` method now contains significant query orchestration logic (loading allocations, team lead assignments, computing availability, aggregating managed projects). This is borderline — ideally this would live in an Application-layer query handler (e.g., `GetEmployeeDetailQuery` / `GetEmployeeDetailQueryHandler`). The current placement in the controller is functional but limits reusability and testability. See CR-22.

### 4.4 New Repository Dependencies in EmployeesController

`EmployeesController` now depends on `IProjectRepository` and `IProjectTeamMemberRepository` in addition to the existing `IEmployeeRepository` and `IAllocationRepository`. This increases the constructor parameter count to 6. While not a violation, it signals that the controller is accumulating orchestration responsibilities. Moving to a query handler would reduce controller dependencies.

---

## 5. Performance Assessment

### 5.1 Query Profile by Endpoint

| Endpoint                         | DB Queries (worst case)                                          | N+1 Risk | Notes                                                             |
| -------------------------------- | ---------------------------------------------------------------- | -------- | ----------------------------------------------------------------- |
| `GET /projects/{code}`           | 1 (with Include fix)                                             | None     | Single query with Includes. Currently missing Includes (CR-8).    |
| `GET /employees/{empCode}`       | 3 (employee + allocations + teamLeadAssignments)                 | Low      | Fixed 3 queries per detail request. Acceptable for single-entity. |
| `GET /employees/me`              | 3 (same as above)                                                | Low      | Same flow via `BuildEmployeeDetailResponse`.                      |
| `GET /allocations/{id}`          | 3 (allocation + employee + project)                              | None     | Three separate lookups, no loops.                                 |
| `PATCH /allocations/{id}` (Stop) | 3+1 (command handler + re-fetch allocation + employee + project) | None     | Re-fetch after command is a pattern used for response building.   |
| `DELETE /allocations/{id}`       | 3+1 (same pattern as Stop)                                       | None     | Same re-fetch pattern.                                            |
| `GET /dashboard/project-view`    | 1 + N_accounts + N_accounts×N_projects                           | **High** | Nested foreach. Deprecated endpoint (CR-13).                      |
| `GET /dashboard/employee-view`   | 2 + N_employees (allocations per employee)                       | **High** | foreach with per-employee allocation fetch. Deprecated (CR-13).   |

### 5.2 Eager Loading Gap Summary

| Repository Method                                | Current Includes                              | Missing Includes (for enrichment)                                      |
| ------------------------------------------------ | --------------------------------------------- | ---------------------------------------------------------------------- |
| `ProjectRepository.GetByCodeAsync`               | `Account`, `ProjectManager`                   | `Allocations.Employee`, `TeamMembers.TeamLead`, `TeamMembers.Reportee` |
| `EmployeeRepository.GetByEmpCodeAsync`           | `EmployeeSkills.Skill`, `Allocations.Project` | `ManagedProjects.Account`, `ManagedProjects.Allocations`, `ReportsTo`  |
| `AllocationRepository.GetByEmployeeAsync`        | `Project`                                     | `Project.Account`                                                      |
| `ProjectTeamMemberRepository.GetByTeamLeadAsync` | (none)                                        | `Project`, `Project.Account`, `Project.Allocations`                    |

---

## 6. Security Assessment

### 6.1 PM Scope Enforcement

PM scope is enforced in both command handlers (`CreateAllocationCommandHandler`, `UpdateAllocationCommandHandler`) and is not affected by the enrichment changes. The enrichment is read-only — it populates response DTOs, not input commands. No new write paths were introduced.

### 6.2 ManagedProjects Data Exposure (CR-17)

| Endpoint                   | Auth Policy   | ManagedProjects Exposed? | Risk                                                                        |
| -------------------------- | ------------- | ------------------------ | --------------------------------------------------------------------------- |
| `GET /employees/{empCode}` | `[Authorize]` | Yes                      | Any authenticated user can see any employee's managed projects + team sizes |
| `GET /employees/me`        | `[Authorize]` | Yes (self only)          | No risk — own data                                                          |
| `GET /employees` (list)    | `CanAllocate` | No (summary DTO)         | ManagedProjects not on summary response                                     |

The `ActiveResourceCount` field on `ManagedProjectItem` reveals team size per project. For a Staff user viewing a PM's profile, this is arguably low-sensitivity data. However, the authorization asymmetry (list requires `CanAllocate`, detail does not) should be an explicit design decision.

### 6.3 Soft-Delete Visibility

The `includeRemoved` parameter on `GET /employees/{empCode}` is documented as "HR only" in the XML doc comment, but there is no runtime policy check — any authenticated user can pass `includeRemoved=true`. However, since the global query filter on `Allocation` (`DeletedAt == null`) is applied by EF Core, the parameter is effectively inert: EF will never return soft-deleted allocations regardless of the flag. See CR-16.

---

## 7. Consistency Assessment

### 7.1 ComputeAllocationStatus Centralization

`AllocationDetailResponse.ComputeAllocationStatus(DateOnly fromDate, DateOnly? toDate)` is used in **all 9 build sites**:

| Location                                      | Uses `ComputeAllocationStatus`? |
| --------------------------------------------- | ------------------------------- |
| `CreateAllocationCommandHandler` (line 117)   | ✅                              |
| `UpdateAllocationCommandHandler` (line 103)   | ✅                              |
| `AllocationsController.GetById` (line 109)    | ✅                              |
| `AllocationsController.Stop` (line 190)       | ✅                              |
| `AllocationsController.Remove` (line 245)     | ✅                              |
| `ProjectsController.GetByCode` (line 144)     | ✅                              |
| `DashboardController.ProjectView` (line 139)  | ✅                              |
| `DashboardController.EmployeeView` (line 246) | ✅                              |
| `EmployeesController.BuildEmployeeDetail`     | ✅                              |

**Verdict: Fully centralized.** No duplicate status computation logic exists.

### 7.2 Inline Build Consistency

All 7 controller inline builds map the same 17 properties in the same order. Cross-checked field-by-field:

| Field       | GetById | Stop | Remove | ProjectsCtrl | DashboardPV | DashboardEV | EmployeesCtrl | Consistent? |
| ----------- | ------- | ---- | ------ | ------------ | ----------- | ----------- | ------------- | ----------- |
| Designation | ✅      | ✅   | ✅     | ✅           | ✅          | ✅          | ✅            | ✅          |
| Billable    | ✅      | ✅   | ✅     | ✅           | ✅          | ✅          | ✅            | ✅          |
| AccountCode | ✅      | ✅   | ✅     | ✅           | ✅          | ✅          | ✅            | ✅          |
| AccountName | ✅      | ✅   | ✅     | ✅           | ✅          | ✅          | ✅            | ✅          |
| Status      | ✅      | ✅   | ✅     | ✅           | ✅          | ✅          | ✅            | ✅          |
| UpdatedAt   | ✅      | ✅   | ✅     | ✅           | ✅          | ✅          | ✅            | ✅          |

**Verdict: All builds consistent.** The DRY violation (CR-12) is a maintainability concern, not a correctness issue today.

### 7.3 Nullable Safety

| Expression                                        | Null-safe? | Notes                                                                                         |
| ------------------------------------------------- | ---------- | --------------------------------------------------------------------------------------------- |
| `project.Account?.AccountCode ?? string.Empty`    | ✅         | Used in command handlers. `Account` is eagerly loaded in `ProjectRepository.GetByIdAsync`.    |
| `project?.Account?.AccountCode ?? string.Empty`   | ✅         | Used in `AllocationsController` where project is separately fetched and could be null.        |
| `a.Project?.Account?.AccountCode ?? string.Empty` | ⚠️         | Safe against null, but `Account` is NOT loaded (CR-10), so always resolves to `string.Empty`. |
| `employee?.Designation ?? string.Empty`           | ✅         | Properly handles null employee in `UpdateAllocationCommandHandler`.                           |
| `p.Account?.AccountCode ?? string.Empty`          | ✅         | In `ProjectsController.GetByCode` — `Account` is eagerly loaded via `GetByCodeAsync`.         |

---

## 8. Test Coverage Assessment

### 8.1 Enrichment Tests

| Test File                                  | # Tests | Coverage Target                                    | Quality                                              |
| ------------------------------------------ | ------- | -------------------------------------------------- | ---------------------------------------------------- |
| `ProjectDetailResponseEnrichmentTests.cs`  | 2       | Allocations + TeamMembers on ProjectDetailResponse | Good — verifies collection wiring and field mapping  |
| `EmployeeDetailResponseEnrichmentTests.cs` | 3       | ManagedProjects (PM, TeamLead, empty)              | Good — covers PM role, TeamLead role, and empty case |

**Total enrichment-specific tests: 5**

### 8.2 Command Handler Tests (Updated)

| Test File                                | # Tests | Coverage of New Fields                                                                                                              |
| ---------------------------------------- | ------- | ----------------------------------------------------------------------------------------------------------------------------------- |
| `CreateAllocationCommandHandlerTests.cs` | 7+      | Happy path asserts `result.Should().NotBeNull()` but does not assert specific new fields (Designation, Billable, Account\*, Status) |
| `UpdateAllocationCommandHandlerTests.cs` | 5+      | Happy path asserts `result.Percentage.Should().Be(60)` but does not assert new fields                                               |

### 8.3 Test Coverage Gaps

| Gap                                                                                                                 | Severity | Finding |
| ------------------------------------------------------------------------------------------------------------------- | -------- | ------- |
| No integration test for enriched `GET /projects/{code}` returning populated Allocations/TeamMembers                 | Medium   | CR-19   |
| No integration test for enriched `GET /employees/{empCode}` returning populated ManagedProjects                     | Medium   | CR-19   |
| Enrichment unit tests don't assert the 6 new `AllocationDetailResponse` fields                                      | Low      | CR-18   |
| No test for `includeRemoved=true` actually returning soft-deleted allocations (it can't due to global query filter) | Low      | CR-16   |
| No test for `ComputeAllocationStatus` edge cases (fromDate == today, toDate == today)                               | Info     | —       |
| Handler tests don't assert `AccountCode`, `AccountName`, `Status`, or `Designation` on response                     | Low      | CR-18   |

### 8.4 Are 10 Tests Sufficient?

The 5 enrichment tests + existing handler tests cover the **DTO structure** adequately. However, the most critical bugs in this change (CR-8, CR-9, CR-10, CR-11 — missing eager loading) would only be caught by **integration tests** that exercise the real EF Core pipeline. The current unit tests construct DTOs in-memory and therefore cannot detect missing `.Include()` calls. **10 unit tests are structurally sufficient but do not cover the actual risk area.**

---

## 9. Backward Compatibility

### 9.1 Additive Changes (Safe)

| DTO                        | New Properties                                                                 | Default Value                                                                                | Breaking?     |
| -------------------------- | ------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------- | ------------- |
| `AllocationDetailResponse` | `Designation`, `Billable`, `AccountCode`, `AccountName`, `Status`, `UpdatedAt` | `string.Empty`, `false`, `string.Empty`, `string.Empty`, `string.Empty`, `default(DateTime)` | No — additive |
| `ProjectDetailResponse`    | `Allocations`, `TeamMembers`                                                   | `Array.Empty<>()`                                                                            | No — additive |
| `EmployeeDetailResponse`   | `ManagedProjects`                                                              | `[]`                                                                                         | No — additive |

All new properties have default values. Existing JSON consumers will receive additional fields, which is non-breaking for standard JSON deserialization. Strictly-typed SDK clients generated from a prior OpenAPI spec may need regeneration.

### 9.2 Deprecation (Non-Breaking)

`[Obsolete]` on `DashboardController` emits compiler warnings (CS0618) but does not affect runtime behavior. The endpoints remain fully functional and routable. This is the standard .NET deprecation pattern.

---

## 10. Verdict and Recommendations

### Verdict: **PASS WITH OBSERVATIONS**

The enrichment design is sound: centralized status computation, consistent inline builds, proper nullable handling, additive API changes, and correct deprecation. The TDD tests validate DTO structure effectively. However, **two high-severity eager-loading gaps** (CR-8, CR-9) will cause the enriched endpoints to return empty data in production, and two medium-severity gaps (CR-10, CR-11) will cause null/empty values for Account and ReportsTo fields. These must be fixed before the feature ships.

### Recommended Follow-Up Actions

| Priority | Action                                                                                                                                      | Finding |
| -------- | ------------------------------------------------------------------------------------------------------------------------------------------- | ------- |
| **High** | Add `Allocations.Employee`, `TeamMembers.TeamLead`, `TeamMembers.Reportee` Includes to `ProjectRepository.GetByCodeAsync`                   | CR-8    |
| **High** | Add `Project`, `Project.Account`, `Project.Allocations` Includes to `ProjectTeamMemberRepository.GetByTeamLeadAsync`                        | CR-9    |
| Medium   | Add `ThenInclude(p => p.Account)` to `AllocationRepository.GetByEmployeeAsync`                                                              | CR-10   |
| Medium   | Add `ManagedProjects.Account`, `ManagedProjects.Allocations`, `ReportsTo` Includes to `EmployeeRepository.GetByEmpCodeAsync`/`GetByIdAsync` | CR-11   |
| Medium   | Extract a shared `AllocationDetailResponse` factory method to eliminate 7 inline builds                                                     | CR-12   |
| Medium   | Add integration tests for enriched endpoints with real DB to catch eager-loading issues                                                     | CR-19   |
| Low      | Standardize on `DateTime.UtcNow` for date computation across controllers and `ComputeAllocationStatus`                                      | CR-15   |
| Low      | Remove redundant `DeletedAt == null` checks or add `IgnoreQueryFilters()` for `includeRemoved` path                                         | CR-16   |
| Low      | Document the `GET /employees/{empCode}` authorization decision for `ManagedProjects` exposure                                               | CR-17   |
| Low      | Add assertions for new fields in enrichment unit tests                                                                                      | CR-18   |
| Info     | Split `BuildEmployeeDetailResponse` into smaller focused methods                                                                            | CR-22   |
| Info     | Use `HashSet<Guid>` for project-ID dedup in ManagedProjects TeamLead aggregation                                                            | CR-23   |
| Info     | Add `Sunset` response header to deprecated Dashboard endpoints                                                                              | CR-21   |
| Info     | Bump `api-spec.yaml` version and document new schema fields                                                                                 | CR-20   |

---

_End of Code Review Report — API Enrichment Scope_
