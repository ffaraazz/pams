# Orchestrator State

## Current Pipeline State: `DEV_VERIFIED_ALL_TESTS_PASS`

### Active Feature: Export Enhancements — Column Map, PDF Borders & Logos — COMPLETE

**Scope:**

1. Export endpoints changed from `GET` to `POST` with optional JSON body containing column name map (`{ "empCode": "Employee Code", ... }`)
2. Only mapped fields appear in export; unmapped fields are hidden. When no body is sent, all default columns are included.
3. PDF exports: table borders, alternating row colors, NexFlow app logo (SVG, left-aligned), NexTurn company logo (PNG, right-aligned), styled footer with generation date
4. Excel exports: styled header row (indigo background), cell borders, Yes/No for booleans
5. Column registries for all 4 entities covering all response DTO fields

**Changes:**

- NEW: `src/PAMS.Application/DTOs/Common/ExportRequest.cs` — `ExportRequest` record with `Dictionary<string, string>? Columns`
- `IExportService.cs`: Added `Dictionary<string, string>? columns = null` parameter to all 4 methods
- `ExportService.cs`: Full rewrite — column registries with `BuildColumnar<T>`, company logo caching, SVG app logo, PDF borders, styled Excel
- All 4 controllers: `[HttpGet("export")]` → `[HttpPost("export")]`, added `[FromBody] ExportRequest?`, passes `exportRequest?.Columns`
- `ExportServiceTests.cs`: Updated all method signature tests for 4 parameters

**Usage Example:**

```
POST /api/v1/employees/export?ext=pdf
Content-Type: application/json

{
  "columns": {
    "empCode": "Employee Code",
    "fullName": "Name",
    "designation": "Title",
    "isActive": "Status"
  }
}
```

**Verification:** 0 errors, 0 warnings, 231/231 tests pass, Docker container rebuilt

---

### Previous Feature: Sort Field Expansion (v2) + Common Error Schema — COMPLETE

**Scope:**

1. Expand SortFields to cover ALL response DTO fields: `projectBillable`, `status` (Allocation); `projectManagerEmpCode` (Project); `totalActiveProjects`, `totalInactiveProjects` (Account)
2. Create `InvalidSortException` with `Field` and `AllowedFields` properties
3. Handle `InvalidSortException` in `ExceptionHandlerMiddleware` returning ProblemDetails with `field` and `allowedFields` extensions
4. Remove all try-catch(ArgumentException) from controllers — middleware handles it
5. Convert ALL `BadRequest(string)` calls to `Problem(...)` returning ProblemDetails (5 occurrences)

**Changes:**

- NEW: `src/PAMS.Application/Exceptions/InvalidSortException.cs`
- `ExceptionHandlerMiddleware.cs`: Added `InvalidSortException` case + extensions
- `AllocationRepository.cs`: +2 sort fields (`projectBillable`, `status`), throws `InvalidSortException`
- `ProjectRepository.cs`: +1 sort field (`projectManagerEmpCode`), throws `InvalidSortException`
- `AccountRepository.cs`: +2 sort fields (`totalActiveProjects`, `totalInactiveProjects`), throws `InvalidSortException`
- `EmployeeRepository.cs`: throws `InvalidSortException`
- All 4 controllers: removed 8 try-catch blocks, converted 5 BadRequest(string) to Problem(...)

**Error Response Format (all endpoints):**

```json
{
  "type": "https://pams.internal/errors/ERR_INVALID_SORT",
  "title": "Invalid Sort Parameter",
  "status": 400,
  "detail": "Invalid sort field 'xyz'. Allowed fields: ...",
  "instance": "/api/v1/...",
  "field": "xyz",
  "allowedFields": ["field1", "field2", ...]
}
```

**Verification:** 0 errors, 0 warnings, 231/231 tests pass, Docker container rebuilt

---

### Previous Feature: Sort Field Expansion (v1) — SUPERSEDED

**Scope:**

1. Add `sort` query parameter to all 4 paginated list APIs (allocations, projects, accounts, employees) - supports field name with optional `-` prefix for descending
2. Add export endpoints for all 4 entities: `GET /api/v1/{entity}/export?ext=pdf|xls` - supports same filter params as list endpoints, returns file download
3. Libraries: ClosedXML (XLS), QuestPDF (PDF)
4. Whitelist sortable fields per entity to prevent arbitrary column access

---

### Previous Feature: Allocation Billable + Simplify /me + GET /allocations + ResourceCount + Date Validation — COMPLETE

**Scope:**

1. Add `Billable` (bool, default true) to Allocation entity — resource-level billable distinct from project-level
2. Simplify `/employees/me` — remove CurrentAllocations & ManagedProjects (no pagination support)
3. Add paginated `GET /allocations` with filters (empCode, projectCode, projectManagerEmpCode, status, billable)
4. Add `ResourceCount` (int) to ProjectSummaryResponse & ProjectDetailResponse
5. Validate `FromDate >= today` on CreateAllocationCommand
6. Delete `ManagedProjectItem.cs` DTO (no longer used)

**Scope:**

1. Add `ProjectRole` (string, nullable) to Allocation entity + DB column
2. Replace `Designation` with `ProjectRole` on `AllocationDetailResponse`
3. Accept `ProjectRole` on POST/PUT allocation commands
4. Update all handlers, MapFrom, tests, and docs

---

## State Transition Log

| Timestamp            | From State              | To State                    | Trigger                                                               |
| -------------------- | ----------------------- | --------------------------- | --------------------------------------------------------------------- |
| 2026-03-03T06:44:00Z | DEVELOPMENT_IN_PROGRESS | DEV_VERIFIED_ALL_TESTS_PASS | POST APIs fixed, 186/186 unit tests pass, all endpoints verified live |

---

## Changes Delivered (This Cycle)

### Bug Fix: `CurrentUserService.EmployeeId` returning `Guid.Empty`

- **Root Cause:** Keycloak JWT contains `empCode` claim but no valid `sub` matching DB employee IDs. `EmployeeId` parsed `sub` → `Guid.Empty`.
- **Impact:** 12 usages across 8 files — all POST/PUT/PATCH/DELETE endpoints failed (FK violation on `allocated_by_id`, broken PM scope checks, broken audit logging, broken dashboard PM filtering).
- **Fix (file: `src/PAMS.Infrastructure/Services/CurrentUserService.cs`):** Resolve `EmployeeId` from DB via `empCode` claim lookup with per-request caching. Falls back to `sub` if it matches a valid employee.
- **Fix (file: `src/PAMS.Application/Commands/Allocations/StopAllocation/StopAllocationCommandHandler.cs`):** When stopping a future allocation, domain service returns `today` (per FR-013 spec), but DB constraint `chk_allocation_dates` requires `toDate >= fromDate`. Handler now uses `fromDate` as `toDate` when `stopDate < fromDate`.

### Verification

- 186/186 unit tests pass
- Live API tested: POST, PUT, PATCH, DELETE all return correct HTTP status codes
- PM scope enforcement verified (403 on non-owned projects, 201 on owned)
- HR full access verified

---

## Parallel Agent Dispatch — 2026-03-03

### Dispatch #1: BusinessAnalyst → `project-notes/specs.md`

- **Task:** Update specs to reflect identity resolution via `empCode` claim. Add NFR-22, update Integration Requirements, update A-04, add FR-013 AC for future allocation stop.
- **Status:** ✅ COMPLETED (2026-03-03T07:15:00Z)
- **Results:** specs.md v1.3.0 — NFR-22 added, Integration/Auth row updated with empCode bridge, A-04 updated, FR-013 AC-013-6 added.

### Dispatch #2: ProductArchitect → `project-notes/architecture.md` + `project-notes/api-spec.yaml`

- **Task:** Update architecture with Identity Resolution Flow section. Update CurrentUserService description in §4.3. Add constraint-safe stop date note in §4.2. Bump api-spec.yaml version.
- **Status:** ✅ COMPLETED (2026-03-03T07:15:00Z)
- **Results:** architecture.md v1.5.0 — §4.3 updated, Identity Resolution Flow section added, StopAllocation constraint note added. api-spec.yaml v1.5.0 — info section updated.

### Dispatch #3: TestEngineer → `tests/**` + `project-notes/test-report.md` + `project-notes/backend-test-report.md`

- **Task:** Add `Handle_FutureAllocation_ShouldSetToDateToFromDate` test to StopAllocationCommandHandlerTests. Update test counts in both reports.
- **Status:** ✅ COMPLETED (2026-03-03T07:15:00Z)
- **Results:** StopAllocationCommandHandlerTests now has 6 tests. test-report.md: 187 unit / 246 total. backend-test-report.md: 101 unit, Bug Fixes section added.

### Dispatch #5: ProductArchitect → `project-notes/api-spec.yaml` (Spec-Implementation Alignment)

- **Task:** Align api-spec.yaml with actual controller implementation. Fix 10 discrepancies: stop allocation description, /me identity resolution, remove unimplemented endpoints, fix response schemas.
- **Status:** ✅ COMPLETED (2026-03-03T08:00:00Z)
- **Results:** api-spec.yaml v1.6.0 — PATCH stop description corrected, GET /me updated with empCode resolution, /employees/me/allocations removed, /employees/me/team/allocations removed, MVP2 endpoints marked spec-only, AllocationDetailResponse trimmed to 11 actual fields, AccountSummary cleaned, unused schemas removed.

---

## Iteration Tracking

| Agent            | Iteration | Status    |
| ---------------- | --------- | --------- |
| BusinessAnalyst  | 1         | COMPLETED |
| ProductArchitect | 2         | COMPLETED |
| TestEngineer     | 1         | COMPLETED |
| CodeReviewer     | 1         | COMPLETED |

---

## Next Transition

- **Target State:** `CODE_REVIEW` → `RELEASE_APPROVED`
- **CodeReviewer Verdict:** PASS WITH OBSERVATIONS (no blocking issues)
- **Wave 2 completed at 2026-03-03T07:30:00Z**
- **Action:** Ready for release approval pending user confirmation

---

## API Enrichment Feature — 2026-03-03

### Wave 1 (parallel — no file overlap)

#### Dispatch #6: BusinessAnalyst → `project-notes/specs.md`

- **Task:** Update specs with enriched DTO requirements (AllocationDetailResponse +6 fields, ProjectDetailResponse +allocations/teamMembers, EmployeeDetailResponse +managedProjects), dashboard deprecation
- **Status:** ✅ COMPLETED (2026-03-03T09:00:00Z)
- **Results:** specs.md v1.4.0 — FR-010 enriched (AC-010-8 to AC-010-12), FR-005 enriched (AC-005-6 to AC-005-8), FR-024 added (Managed Projects, 6 ACs), dashboard deprecated in §13, master index updated.

#### Dispatch #7: ProductArchitect → `project-notes/api-flow.md` + `project-notes/api-spec.yaml` + `project-notes/architecture.md`

- **Task:** Create api-flow.md (role-based API consumption guide), update api-spec.yaml with enriched response schemas, update architecture.md with new data flow
- **Status:** ✅ COMPLETED (2026-03-03T09:00:00Z)
- **Results:** api-flow.md v1.0.0 created (7 sections, 29 endpoints mapped). api-spec.yaml v1.7.0 (AllocationDetailResponse +6, ProjectDetailResponse +allocations/teamMembers, EmployeeDetailResponse +managedProjects, ManagedProjectItem schema, dashboard deprecated). architecture.md updated with Enriched Response Strategy §16.

### Wave 2 (sequential — depends on specs + api-spec)

#### Dispatch #8: TestEngineer → `tests/**`

- **Task:** Write failing tests for enriched DTOs (AllocationDetailResponse new fields, ProjectDetailResponse embedded allocations/teamMembers, EmployeeDetailResponse managedProjects)
- **Status:** ✅ COMPLETED (2026-03-03T09:15:00Z)
- **Results:** 10 failing tests across 3 files: CreateAllocationCommandHandlerTests.cs (+5 tests), ProjectDetailResponseEnrichmentTests.cs (new, 2 tests), EmployeeDetailResponseEnrichmentTests.cs (new, 3 tests). All fail to compile — TDD red phase.

### Wave 3 (sequential — depends on failing tests)

#### Dispatch #9: BackendDeveloper → `src/**`

- **Task:** Enrich DTOs + update handlers/controllers to pass all 10 failing tests. Deprecate DashboardController.
- **Status:** ✅ COMPLETED (2026-03-03T09:30:00Z)
- **Results:** 1 file created (ManagedProjectItem.cs), 9 files modified. AllocationDetailResponse +6 fields with ComputeAllocationStatus helper. ProjectDetailResponse +Allocations/TeamMembers. EmployeeDetailResponse +ManagedProjects (PM + TeamLead). DashboardController marked [Obsolete]. Zero compile errors.

### Wave 4 (parallel — independent)

#### Dispatch #10: TestEngineer (QA mode) → `project-notes/test-report.md`

- **Task:** Validate all changes, verify enriched DTOs, update test counts
- **Status:** ✅ COMPLETED (2026-03-03T09:45:00Z)
- **Results:** All 10 tests validated. Counts: 197 unit / 256 total. 3 low-priority gaps noted.

#### Dispatch #11: CodeReviewer → `project-notes/code-review-report.md`

- **Task:** Review all enriched DTO changes for quality, security, performance
- **Status:** ✅ COMPLETED (2026-03-03T09:45:00Z)
- **Results:** PASS WITH OBSERVATIONS. 2 High (missing EF Includes), 4 Medium, 6 Low, 4 Info. Highs must be fixed.

### Wave 5 (fix code review findings)

#### Dispatch #12: BackendDeveloper → `src/**`

- **Task:** Fix CR-8, CR-9, CR-10, CR-11 (missing EF Core Includes). Fix CR-12 (DRY — centralize allocation mapping).
- **Status:** ✅ COMPLETED (2026-03-03T10:00:00Z)
- **Results:** 10 files modified: 3 repositories (Includes added), 1 DTO (MapFrom centralized), 6 controllers/handlers (inline builds → MapFrom). Zero compile errors. All High/Medium review findings resolved.

### Wave 6 (parallel — dashboard removal)

#### Dispatch #13: BackendDeveloper → `src/**` + `tests/**`

- **Task:** Remove DashboardController, DashboardResponses, DashboardEndpointTests, CanViewDashboard policy
- **Status:** ✅ COMPLETED (2026-03-03T10:30:00Z)
- **Results:** 3 files gutted (DashboardController.cs, DashboardResponses.cs, DashboardEndpointTests.cs), 1 file edited (ServiceCollectionExtensions.cs — CanViewDashboard removed). Zero dangling references. Zero compile errors.

#### Dispatch #14: ProductArchitect → `project-notes/api-spec.yaml` + `project-notes/api-flow.md` + `project-notes/architecture.md`

- **Task:** Remove all dashboard references from documentation
- **Status:** ✅ COMPLETED (2026-03-03T10:30:00Z)
- **Results:** api-spec.yaml v1.8.0 (dashboard tag, 2 paths, 5 schemas removed). api-flow.md (29→27 endpoints, deprecation→removal section). architecture.md (CanViewDashboard policy removed, dashboard controller/queries removed, §16 updated to "Dashboard Removal").

---

### Wave 7 — Seeder Update (ProjectRole Population)

#### Dispatch #15: BackendDeveloper → `src/PAMS.Infrastructure/Persistence/Seed/DemoDataSeeder.cs`

- **Task:** Add `ProjectRole` values to all 12 allocation seed entries. Update integration test SeedData if needed.
- **Status:** ✅ COMPLETED (2026-03-03T11:15:00Z)
- **Dispatched:** 2026-03-03T11:00:00Z
- **Results:** All 12 allocation seeds updated with ProjectRole values. Zero compile errors. Values: Senior Developer, Developer, Tech Lead, Architect, Frontend Developer, QA Lead, QA Engineer, DevOps Engineer (×2), Backend Developer, Developer, Full Stack Developer.

---

## Allocation Billable + Simplify /me + GET /allocations + ResourceCount + Date Validation — 2026-03-04

### Wave 8 (parallel — specs + architecture)

#### Dispatch #16: BusinessAnalyst → `project-notes/specs.md`

- **Task:** Update specs v1.6.0 with 6 requirement changes (FR-010 Billable + fromDate, FR-012 Billable, FR-005 ResourceCount, FR-024 REMOVED, FR-025 new paginated allocations)
- **Status:** ✅ COMPLETED (2026-03-04T09:00:00Z)

#### Dispatch #17: ProductArchitect → `project-notes/api-spec.yaml` + `project-notes/api-flow.md` + `project-notes/architecture.md`

- **Task:** api-spec.yaml v1.10.0, api-flow.md v1.1.0, architecture.md v1.6.0 — new GET /allocations, Billable schemas, ResourceCount, simplified employee endpoint
- **Status:** ✅ COMPLETED (2026-03-04T09:00:00Z)

### Wave 9 (sequential — TDD red)

#### Dispatch #18: TestEngineer → `tests/**`

- **Task:** Write 11 failing tests across 5 files for all features
- **Status:** ✅ COMPLETED (2026-03-04T09:15:00Z)
- **Results:** 3 files created, 2 files modified. 11 tests: Billable (3), fromDate validation (2), employee simplification (2), ResourceCount (2), filtered repo (2)

### Wave 10 (sequential — TDD green)

#### Dispatch #19: BackendDeveloper → `src/**`

- **Task:** Implement all 6 features (18 files modified, 1 created)
- **Status:** ✅ COMPLETED (2026-03-04T09:30:00Z)
- **Results:** Entity Billable, Commands, Handlers, DTOs, Controllers, Repository filtered queries, Validator fromDate, Migration, Seeder, EF Config. Zero compile errors.

### Wave 11 (parallel — QA + review)

#### Dispatch #20: TestEngineer (QA) → `project-notes/test-report.md`

- **Task:** Validate all 11 tests, update test counts
- **Status:** ✅ COMPLETED (2026-03-04T09:45:00Z)
- **Results:** 207 unit / 266 total. All validations passed. 6 gaps noted.

#### Dispatch #21: CodeReviewer → `project-notes/code-review-report.md`

- **Task:** Review all changes
- **Status:** ✅ COMPLETED (2026-03-04T09:45:00Z)
- **Results:** PASS WITH OBSERVATIONS. 1 Critical (CR-24: role-scoping), 1 High (CR-25: UpdateValidator), 4 Medium, 6 Low, 4 Info.

### Wave 12 (fix review findings)

#### Dispatch #22: BackendDeveloper → `src/**`

- **Task:** Fix CR-24 (role-scoping GET /allocations), CR-25 (create UpdateAllocationCommandValidator), CR-26/27 (dead params), CR-28 (audit log Billable/ProjectRole)
- **Status:** ✅ COMPLETED (2026-03-04T10:00:00Z)
- **Results:** 5 files modified, 1 file created. Role-scoping: Staff→own, PM→own projects, HR→all. UpdateAllocationCommandValidator with percentage, date, increment rules. Dead params removed. Audit logs enriched.

---

## Seeder Data Quality Fix — 2026-03-04

### Wave 13

#### Dispatch #23: BackendDeveloper → `src/PAMS.Infrastructure/Persistence/Seed/EmployeeSeeder.cs` + `DemoDataSeeder.cs`

- **Task:** Fix seed data for correctness and realism
- **Status:** ✅ COMPLETED (2026-03-04T11:00:00Z)
- **Results:**
  - EmployeeSeeder: EMP-001 "HR Admin" → "Priya Sharma", EMP-002 "Alice PM" → "Alice Morgan", EMP-003 "Bob Staff" → "Bob Reynolds" (emails updated)
  - DemoDataSeeder: PRJ-TEAMS-INT status Active→Completed (end date 2025-12-31 is past), PRJ-FRAUD-DET status Upcoming→Active (start date 2026-01-01 is past)
  - Reporting lines: EMP-008 (James Taylor) + EMP-009 (Rachel Martinez) now report to EMP-001 (Priya Sharma, HR)
  - Skills: EMP-003 (Bob) gained C# and .NET (works on .NET projects)

---

## Validator Fix + Date Format — 2026-03-04

### Wave 14 (parallel)

#### Dispatch #24: BackendDeveloper → `src/PAMS.Application/Validators/UpdateAllocationCommandValidator.cs` + `src/PAMS.API/Program.cs`

- **Task:** Remove `FromDate >= today` from UpdateAllocationCommandValidator (conflicts with editing active allocations). Add `MapType<DateOnly>` to Swagger config for proper OpenAPI date format.
- **Status:** ✅ COMPLETED (2026-03-04T12:00:00Z)
- **Results:** Removed past-date rule from Update validator (handler already guards ended allocations). Added `MapType<DateOnly>` to `AddSwaggerGen` — generates `{ type: "string", format: "date", example: "YYYY-MM-DD" }` for all DateOnly properties in Swagger UI.

#### Dispatch #25: ProductArchitect → `project-notes/api-spec.yaml`

- **Task:** Add ISO 8601 `example` values to all date/date-time fields in component schemas
- **Status:** ✅ COMPLETED (2026-03-04T12:00:00Z)
- **Results:** 11 date examples added across AllocationDetailResponse, CreateAllocationRequest, UpdateAllocationRequest, CapacityCheckResponse, CapacityExceededProblem schemas.

---

## DB-Driven Role Resolution (IdP-Agnostic Auth) — 2026-03-04

### Wave 17 (parallel specs + sequential implementation)

#### Dispatch #26: BusinessAnalyst → `project-notes/specs.md`

- **Task:** Update specs for DB-driven authorization, IdP-agnostic auth model
- **Status:** ✅ COMPLETED (2026-03-04T14:00:00Z)
- **Results:** specs.md v1.7.0 — NFR-23 added (IdP-agnostic), NFR-22 updated (empCode only claim), A-04 rewritten, Integration/Auth section updated. All Keycloak-specific role references removed.

#### Dispatch #27: ProductArchitect → `project-notes/architecture.md`

- **Task:** Update architecture for DB-driven auth flow
- **Status:** ✅ COMPLETED (2026-03-04T14:00:00Z)
- **Results:** architecture.md v1.7.0 — Identity Resolution Flow rewritten, §5 Auth fully rewritten, MapKeycloakRolesToClaims references removed, custom IAuthorizationHandler documented.

#### Dispatch #28: BackendDeveloper → `src/**`

- **Task:** Implement DB-driven role resolution
- **Status:** ✅ COMPLETED (2026-03-04T14:15:00Z)
- **Results:**
  - `CurrentUserService.cs` — Role + EmployeeId resolved from DB in single query via `EnsureResolved()`, cached per request
  - `ServiceCollectionExtensions.cs` — Removed `OnTokenValidated`/`MapKeycloakRolesToClaims`, removed `System.Text.Json` import, replaced `RequireRole` policies with custom `DbRoleRequirement`
  - `DbRoleRequirement.cs` (NEW) — `IAuthorizationRequirement` with `AllowedRoles`
  - `DbRoleAuthorizationHandler.cs` (NEW) — `AuthorizationHandler` checking `ICurrentUserService.Role` from DB
  - `ICurrentUserService.cs` — Updated XML doc comment
  - Zero compile errors. ICurrentUserService interface unchanged — all consumers unaffected.

#### Dispatch #29: UIDeveloper → `keycloak/themes/nexflow/login/**`

- **Task:** Complete login page revamp — left-aligned split layout, dual logos (Nexturn company + NexFlow app), Stitch design tokens (#5048e5 primary, #121121 bg), favicon fix
- **Status:** ✅ COMPLETED (2026-03-05T10:00:00Z)
- **Results:**
  - `nexflow.css` — Full rewrite: split layout (480px left panel), gradient overlay (dark→transparent L→R), bg image visible on right, new color scheme from Stitch tokens
  - `login.ftl` — Full rewrite: dual logos (Nexturn company logo + NexFlow app icon SVG + "NexFlow" text + "Manager Allocation Console" tagline), transparent card, left-aligned
  - `login-reset-password.ftl` — Full rewrite: same brand section and layout as login
  - `favicon.svg` — Updated: solid #5048e5 fill, 44x44 viewBox, matching app icon
  - Temp files cleaned up: stitch-screen.json, stitch-design.html, bg.jpg

---

## Sort Parameter + Export APIs (PDF/XLS) — 2026-03-06

### Wave 1 (parallel — specs + architecture)

#### Dispatch #30: BusinessAnalyst → `project-notes/specs.md`

- **Task:** Add FR-026 (Sort Parameter) and FR-027 (Export APIs) with acceptance criteria
- **Status:** ✅ COMPLETED (2026-03-06T12:00:00Z)
- **Results:** specs.md updated — FR-026 (14 ACs), FR-027 (18 ACs), master index updated

#### Dispatch #31: ProductArchitect → `project-notes/architecture.md` + `project-notes/api-spec.yaml` + `project-notes/api-flow.md`

- **Task:** architecture.md §19 (Sort & Export), api-spec.yaml v1.11.0, api-flow.md v1.2.0
- **Status:** ✅ COMPLETED (2026-03-06T12:00:00Z)
- **Results:** SortHelper design, IExportService interface, ExportService design, GetFilteredAllAsync, sort field whitelists, export column definitions, data flow diagrams. 4 export endpoints + sort param on api-spec.

### Wave 2 (sequential — TDD red)

#### Dispatch #32: TestEngineer → `tests/**`

- **Task:** Write 21 failing tests (SortHelperTests 7, SortEndpointTests 8, ExportServiceTests 6)
- **Status:** ✅ COMPLETED (2026-03-06T12:15:00Z)
- **Results:** 3 test files created. All fail to compile/assert (TDD red phase).

### Wave 3 (sequential — TDD green)

#### Dispatch #33: BackendDeveloper → `src/**`

- **Task:** Implement sort parameter + export infrastructure (SortHelper, IExportService, ExportService, repository changes, controller sort params)
- **Status:** ✅ COMPLETED (2026-03-06T12:30:00Z)
- **Results:** 19 files created/modified. SortHelper, IExportService, ExportService (ClosedXML + QuestPDF), 4 repo interfaces + implementations updated, 4 controllers with sort param, DI registration, QuestPDF license.

#### Dispatch #34: BackendDeveloper → `src/PAMS.API/Controllers/**`

- **Task:** Add [HttpGet("export")] endpoints to all 4 controllers
- **Status:** ✅ COMPLETED (2026-03-06T12:45:00Z)
- **Results:** Export endpoints on all 4 controllers with role scoping, format validation (xls→xlsx), GetFilteredAllAsync, File() response.

#### Orchestrator fixes:

- Added `<param name="sort">` XML doc tags to 3 controllers (AccountsController, EmployeesController, ProjectsController)
- Added System.IO.Packaging 9.0.6 override in Directory.Packages.props to fix ClosedXML transitive vulnerability
- Added System.IO.Packaging PackageReference in PAMS.Infrastructure.csproj

### Wave 4 (parallel — QA + review)

#### Dispatch #35: TestEngineer (QA) → `project-notes/test-report.md`

- **Task:** Validate all tests, update report
- **Status:** ✅ COMPLETED (2026-03-06T13:00:00Z)
- **Results:** 231/231 unit tests pass. 7 test gaps documented (#16–#22).

#### Dispatch #36: CodeReviewer → `project-notes/code-review-report.md`

- **Task:** Review all sort + export changes
- **Status:** ✅ COMPLETED (2026-03-06T13:00:00Z)
- **Results:** PASS WITH OBSERVATIONS. 0 Critical, 0 High, 4 Medium, 7 Low, 8 Info. No blocking issues.

### Build Verification

- ✅ `dotnet build`: 0 errors, 0 warnings
- ✅ `dotnet test tests/PAMS.UnitTests`: 231/231 passed

---

## API Spec Alignment (v1.12.0) — 2026-03-05

### Dispatch #37: ProductArchitect (Orchestrator-executed) → `project-notes/api-spec.yaml`

- **Task:** Align api-spec.yaml with implementation — export POST+body, InvalidSort schema, version bump
- **Status:** ✅ COMPLETED (2026-03-05T12:00:00Z)
- **Changes:**
  1. Version bump: 1.11.0 → 1.12.0
  2. Info description: Export endpoints `GET` → `POST`, added column map + sort validation description
  3. All 4 export endpoints: `get:` → `post:` with `requestBody` (optional `ExportColumnMap`)
  4. New schema: `ExportColumnMap` — `additionalProperties: string`, nullable, for column name mapping
  5. New schema: `InvalidSortProblem` — ProblemDetails + `field` + `allowedFields` extensions
  6. New response: `InvalidSortError` — 400 with `InvalidSortProblem` schema + example
  7. Added `"400"` response referencing `InvalidSortError` to all 4 list endpoints
  8. Updated `SortParam` description: mentions ProblemDetails response with `field`/`allowedFields`
- **Verification:**
  - Live Swagger (http://localhost:5000/swagger/v1/swagger.json) confirms all 4 exports are `POST`
  - Request body correctly shows `{ additionalProperties: string }` for export column map
  - Query params (search, isActive, accountType, sort, ext) match spec and implementation
