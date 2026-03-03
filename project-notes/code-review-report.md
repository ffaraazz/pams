# PAMS — Code Review Report

---

## 1. Document Control

| Field       | Value                                                                                                                                        |
| ----------- | -------------------------------------------------------------------------------------------------------------------------------------------- |
| Project     | Project Allocation Management System (PAMS)                                                                                                  |
| Review Date | 2026-03-03                                                                                                                                   |
| Reviewer    | CodeReviewer (GitHub Copilot)                                                                                                                |
| Scope       | Bug fixes #10 (CurrentUserService identity resolution), #11 (StopAllocation constraint-safe stop date), new unit test, documentation updates |
| Verdict     | **PASS WITH OBSERVATIONS**                                                                                                                   |

---

## 2. Executive Summary

Two bug fixes were applied: (1) `CurrentUserService` now resolves employee identity via a multi-step strategy (`sub` claim → DB verify → `empCode` fallback → cache), and (2) `StopAllocationCommandHandler` guards against DB constraint `chk_allocation_dates` violation when stopping future allocations. Both fixes are correct, well-documented, and tested. The changes respect Clean Architecture boundaries and are traceable to updated requirements (NFR-22, AC-013-6, A-04).

**Seven observations** are raised — none are blocking. The most notable are the use of synchronous EF Core calls inside a property getter and the `Guid.Empty` sentinel value when identity resolution fails (silent failure). Both should be addressed in a follow-up iteration.

**Overall Verdict: PASS WITH OBSERVATIONS**

---

## 3. Findings Table

| ID   | Severity | Category      | File(s)                                                                                                 | Description                                                                                                                                                                                                                                                                                                  | Recommendation                                                                                                                                                                                       |
| ---- | -------- | ------------- | ------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| CR-1 | Medium   | Performance   | `CurrentUserService.cs`                                                                                 | `EmployeeId` property getter uses synchronous EF Core calls (`.Any()`, `.FirstOrDefault()`) which block the calling thread. In an async ASP.NET Core pipeline, this occupies a thread-pool thread during the DB round-trip.                                                                                  | Introduce an async method `ResolveEmployeeIdAsync()` on `ICurrentUserService` or use a middleware/filter to pre-resolve identity before handlers execute.                                            |
| CR-2 | Medium   | Security      | `CurrentUserService.cs`                                                                                 | When both `sub` and `empCode` resolution fail, the service returns `Guid.Empty` silently. Downstream handlers (PM scope, audit log `allocatedById`) will receive `Guid.Empty` without any exception, potentially allowing unauthenticated/misconfigured users to execute operations with a phantom identity. | Throw an `UnauthorizedAccessException` or return a result type when identity cannot be resolved. At minimum, log a warning when falling back to `Guid.Empty`.                                        |
| CR-3 | Low      | Architecture  | `Program.cs`, `InfrastructureServiceExtensions.cs`                                                      | `CurrentUserService` is registered in `Program.cs` (line 34) instead of `InfrastructureServiceExtensions.AddInfrastructure()` where all other infrastructure services (`AuditLogService`, `DateTimeProvider`) are registered. This creates a split registration pattern.                                     | Move the `ICurrentUserService` registration into `InfrastructureServiceExtensions` for consistency. `AddHttpContextAccessor()` can remain in the API layer.                                          |
| CR-4 | Low      | Test Coverage | `tests/PAMS.UnitTests/`                                                                                 | No dedicated unit tests for `CurrentUserService` itself. The multi-step identity resolution logic (sub verification, empCode fallback, caching, `Guid.Empty` fallback) is only exercised through integration tests. This is the most complex Infrastructure service.                                         | Add unit tests for `CurrentUserService` using an in-memory DbContext and a mocked `IHttpContextAccessor` covering: valid sub, invalid sub + valid empCode, no claims, caching behavior.              |
| CR-5 | Low      | Code Quality  | `CurrentUserService.cs`                                                                                 | The `sub` claim is looked up via two different claim types (`ClaimTypes.NameIdentifier` and `"sub"`). While this handles ASP.NET Core's default claim mapping and raw JWT claim names, it should be documented inline why both are needed to avoid future confusion.                                         | Add a brief inline comment explaining that ASP.NET Core maps `sub` to `ClaimTypes.NameIdentifier` by default, and the raw `"sub"` is a fallback for configurations where `MapInboundClaims = false`. |
| CR-6 | Info     | Documentation | `specs.md` (v1.3.0), `architecture.md` (v1.5.0), `api-spec.yaml` (v1.5.0), `best-practices.md` (v1.4.0) | Document version numbers follow independent versioning per document, which is acceptable. However, there is no cross-reference table mapping document versions to a single release version. This makes it harder to identify which combination of document versions represents a consistent system state.    | Consider adding a "Document Version Matrix" to one of the documents mapping release version → document versions.                                                                                     |
| CR-7 | Info     | Code Quality  | `StopAllocationCommandHandler.cs`                                                                       | The ternary guard on line 75 (`stopDate < allocation.FromDate ? allocation.FromDate : stopDate`) is correct and concise. The inline comment above it clearly explains the rationale and references the DB constraint. Good practice.                                                                         | No action needed. Documenting the constraint name inline is a best practice.                                                                                                                         |

---

## 4. Architecture Compliance

### 4.1 Dependency Direction

| Layer          | Allowed Dependencies                            | Actual Dependencies in Changed Files                                                   | Compliant? |
| -------------- | ----------------------------------------------- | -------------------------------------------------------------------------------------- | ---------- |
| Domain         | None (BCL only)                                 | Not changed                                                                            | ✅         |
| Application    | Domain                                          | Domain entities, enums, repos, services                                                | ✅         |
| Infrastructure | Domain + Application                            | `PamsDbContext`, `IHttpContextAccessor`, `ICurrentUserService` (Application interface) | ✅         |
| API            | Application + Infrastructure (composition root) | Registers DI; no direct domain logic                                                   | ✅         |

### 4.2 CurrentUserService — PamsDbContext Dependency

The addition of `PamsDbContext` as a constructor dependency in `CurrentUserService` is **architecturally correct**. Infrastructure services are permitted to depend on other Infrastructure components (the DbContext). The interface `ICurrentUserService` remains in the Application layer with no Infrastructure leakage. Handlers depend only on the interface, preserving testability and the Dependency Inversion Principle.

### 4.3 DI Registration (Finding CR-3)

`CurrentUserService` is registered as `Scoped` in `Program.cs` (line 34):

```csharp
builder.Services.AddScoped<ICurrentUserService, PAMS.Infrastructure.Services.CurrentUserService>();
```

All other infrastructure services are registered in `InfrastructureServiceExtensions.AddInfrastructure()`. The split registration works correctly but breaks the single-location convention established by the project. The `Scoped` lifetime is correct — it aligns with `PamsDbContext`'s `Scoped` lifetime and enables per-request caching of `_cachedEmployeeId`.

### 4.4 StopAllocationCommandHandler

The handler remains in the Application layer and correctly delegates stop-date computation to `AllocationStopService` (Domain). The constraint-safe guard (`stopDate < allocation.FromDate ? allocation.FromDate : stopDate`) is applied **in the handler**, not in the domain service. This is appropriate because the guard addresses a persistence-layer constraint (`chk_allocation_dates`), which is an infrastructure concern that the Application layer mediates.

---

## 5. Security Assessment

### 5.1 Identity Resolution Flow

```
JWT arrives → sub claim → parse GUID → DB verify → ✅ use it
                                        ↓ not found
                         empCode claim → DB lookup by EmpCode → ✅ use it
                                                                 ↓ not found
                                                           return Guid.Empty ⚠️
```

| Check                                                | Status | Notes                                                                                                                                                                                                                       |
| ---------------------------------------------------- | ------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| JWT validation (signature, expiry, issuer, audience) | ✅     | Handled by ASP.NET Core `JwtBearerMiddleware` before any claims are read. Configured in `ServiceCollectionExtensions.cs`.                                                                                                   |
| Claim spoofing prevention                            | ✅     | Claims originate from a signed JWT validated against Keycloak's JWKS endpoint. Spoofing requires the Keycloak signing key.                                                                                                  |
| Sub claim verification                               | ✅     | Not blindly trusted — verified against `employees` table before use.                                                                                                                                                        |
| EmpCode claim verification                           | ✅     | Looked up in the `employees` table. If no match, falls through to `Guid.Empty`.                                                                                                                                             |
| Role claim mapping                                   | ✅     | `realm_access.roles` extracted and mapped to `ClaimTypes.Role` in `JwtBearerEvents.OnTokenValidated`. Server-side enforcement via policies.                                                                                 |
| Missing identity fallback                            | ⚠️     | Returns `Guid.Empty` silently (Finding CR-2). No exception, no logging. Downstream PM scope checks will likely fail safely (no project has PM = `Guid.Empty`), but audit log entries will record a phantom `allocatedById`. |

### 5.2 Risk: Claim Spoofing via empCode

**Risk Level: Low.** The `empCode` claim is embedded in the JWT signed by Keycloak. An attacker cannot inject a false `empCode` without compromising the Keycloak signing key or the JWKS endpoint. The JWT validation middleware (JWKS auto-discovery, RS256 signature verification) provides adequate protection. This matches the approach documented in specs.md A-04 and architecture.md §5.

### 5.3 Risk: Guid.Empty Phantom Identity

**Risk Level: Medium.** If identity resolution returns `Guid.Empty`, handlers that use `_currentUser.EmployeeId` for PM scope enforcement will compare against `Guid.Empty`. Since no real project has `ProjectManagerId = Guid.Empty`, PM scope checks will correctly block unauthorized access (the comparison will fail, throwing `ForbiddenException`). However, if the user has the HR role (which bypasses PM scope checks), operations will proceed with `Guid.Empty` as the identity — audit logs will record a non-existent employee as the actor. **Recommendation:** Fail fast with an exception when identity cannot be resolved.

---

## 6. Performance Assessment

### 6.1 Per-Request Caching

`CurrentUserService` caches the resolved `Guid` in `_cachedEmployeeId` (a private field). Since the service is registered as `Scoped`, this field lives for the duration of one HTTP request. Multiple accesses to `EmployeeId` within the same request (e.g., scope check + audit log) hit the cache, not the database. **This is correct and sufficient.**

### 6.2 DB Queries per Request

| Scenario                         | DB Queries | Notes                                           |
| -------------------------------- | ---------- | ----------------------------------------------- |
| Sub claim matches employee       | 1          | `Employees.Any(e => e.Id == subGuid)`           |
| Sub claim invalid, empCode hit   | 2          | `.Any()` fails + `.FirstOrDefault()` by EmpCode |
| Both miss (Guid.Empty fallback)  | 2          | Both queries execute, both miss                 |
| Subsequent access (same request) | 0          | Cached via `_cachedEmployeeId`                  |

**N+1 Risk:** None. At most 2 simple queries per request (indexed by PK and unique column respectively). The `employees.employee_id` column is the PK, and `employees.emp_code` has a unique index.

### 6.3 Synchronous DB Access (Finding CR-1)

The `EmployeeId` property getter uses synchronous EF Core methods (`.Any()`, `.FirstOrDefault()`). In a high-concurrency scenario, this blocks thread-pool threads during the DB round-trip. The impact is proportional to database latency. For typical PAMS load (≤100 concurrent users per NFR-04), this is acceptable. For higher scale, an async pre-resolution pattern should be adopted.

---

## 7. Test Coverage Assessment

### 7.1 StopAllocationCommandHandler Tests (6 total)

| #   | Test Name                                                | Scenario                               | FR/AC Traced | Verdict |
| --- | -------------------------------------------------------- | -------------------------------------- | ------------ | ------- |
| 1   | Handle_ActiveAllocation_ShouldSetToDateAndPersist        | Happy path — active allocation stopped | AC-013-2     | ✅ Good |
| 2   | Handle_PmNotProjectOwner_ShouldThrowForbiddenException   | PM scope enforcement                   | AC-013-5     | ✅ Good |
| 3   | Handle_AllocationNotFound_ShouldThrowNotFoundException   | Missing allocation                     | Edge case    | ✅ Good |
| 4   | Handle_AlreadyEndedAllocation_ShouldThrowDomainException | Already ended                          | Edge case    | ✅ Good |
| 5   | Handle_SuccessfulStop_ShouldWriteAuditLog                | Audit log written                      | AC-013-4     | ✅ Good |
| 6   | Handle_FutureAllocation_ShouldSetToDateToFromDate        | **NEW** — constraint-safe future stop  | AC-013-6     | ✅ Good |

### 7.2 New Test Quality Assessment

The new test `Handle_FutureAllocation_ShouldSetToDateToFromDate`:

- **Arrange:** Creates an allocation with `fromDate = today + 30 days` (clearly future). Uses HR role to bypass PM scope checks, focusing the test on the date logic.
- **Act:** Calls `Handle()` with the command.
- **Assert:** Verifies `allocation.ToDate = futureFromDate` — directly asserts the constraint-safe guard behavior.
- **Naming:** Follows `Method_Scenario_ExpectedResult` convention per best-practices.md §3.
- **Display name:** Includes FR-ID prefix (`FR-013 |`) for traceability.
- **Isolation:** Properly uses NSubstitute; no shared mutable state.

**Quality: High.** The test is focused, correctly targets the bug fix, and follows project conventions.

### 7.3 Test Coverage Gaps

| Gap                                                   | Severity | Notes                                                                                    |
| ----------------------------------------------------- | -------- | ---------------------------------------------------------------------------------------- |
| No unit tests for `CurrentUserService` (Finding CR-4) | Low      | Most complex Infrastructure service; multi-path resolution logic untested at unit level  |
| No test for `stopDate == allocation.FromDate` edge    | Info     | The exact boundary case where `stopDate` equals `FromDate` — would hit the `else` branch |
| No test for HR stopping a future allocation           | Info     | Test uses HR role, which is fine, but no PM + own project variant for future allocation  |

### 7.4 Test Counts Verification

| Report                                          | Unit Tests | Total Tests | Match?                  |
| ----------------------------------------------- | ---------- | ----------- | ----------------------- |
| test-report.md                                  | 187        | 246         | ✅                      |
| backend-test-report.md                          | 101        | —           | ✅                      |
| StopAllocationCommandHandlerTests (actual file) | 6 tests    | —           | ✅ Matches both reports |

---

## 8. Documentation Consistency

### 8.1 Version Matrix

| Document               | Version | Date       | Changes Referencing Bug Fixes                                                                      |
| ---------------------- | ------- | ---------- | -------------------------------------------------------------------------------------------------- |
| specs.md               | 1.3.0   | 2026-03-03 | NFR-22 (identity resolution), AC-013-6 (constraint-safe stop), A-04 (empCode assumption)           |
| architecture.md        | 1.5.0   | 2026-03-03 | Identity Resolution Flow section, §4.3 CurrentUserService description, StopAllocation handler note |
| api-spec.yaml          | 1.5.0   | 2026-03-03 | Info section: identity resolution description                                                      |
| test-report.md         | —       | 2026-03-03 | 187 unit / 246 total; StopAllocationCommandHandlerTests = 6                                        |
| backend-test-report.md | —       | 2026-03-03 | 101 unit; Bug Fixes #10, #11 documented                                                            |
| best-practices.md      | 1.4.0   | 2026-02-26 | Not updated (no changes required)                                                                  |

### 8.2 Cross-Reference Validity

| Reference                               | Source Document        | Target Document                                 | Valid? |
| --------------------------------------- | ---------------------- | ----------------------------------------------- | ------ |
| NFR-22 (User Identity Resolution)       | specs.md §9            | architecture.md §4.3, api-spec.yaml info        | ✅     |
| AC-013-6 (constraint-safe stop date)    | specs.md §8 FR-013     | architecture.md §4.2 (handler note)             | ✅     |
| A-04 (Keycloak empCode requirement)     | specs.md §14           | architecture.md §5 (claim mapping table)        | ✅     |
| `chk_allocation_dates` constraint       | architecture.md §7.2   | StopAllocationCommandHandler.cs line 74 comment | ✅     |
| FR-013 test count = 6                   | test-report.md §1.3    | StopAllocationCommandHandlerTests.cs (actual)   | ✅     |
| Bug Fix #10 (CurrentUserService)        | backend-test-report.md | CurrentUserService.cs (actual)                  | ✅     |
| Bug Fix #11 (StopAllocation constraint) | backend-test-report.md | StopAllocationCommandHandler.cs (actual)        | ✅     |

### 8.3 Inconsistencies Found

| Issue                                                                                                                      | Severity | Notes                                                                                                                                                                                                                              |
| -------------------------------------------------------------------------------------------------------------------------- | -------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| specs.md AC-013-1 says "toDate is set to today" for future allocations, but AC-013-6 overrides this to "toDate = fromDate" | Info     | AC-013-6 is explicitly called out as an addendum to AC-013-1. The two ACs together are consistent, though the wording of AC-013-1 in isolation is incomplete. This is acceptable since AC-013-6 is the constraint-safe refinement. |
| `InfrastructureServiceExtensions.cs` does not register `CurrentUserService`; `Program.cs` registers it directly            | Low      | See Finding CR-3. Functionally correct, but inconsistent registration pattern.                                                                                                                                                     |

---

## 9. Requirement Traceability Matrix

### FR-013 — Stop Allocation

| Requirement | Artifact                                | Location                                            | Status |
| ----------- | --------------------------------------- | --------------------------------------------------- | ------ |
| AC-013-1    | AllocationStopService (Domain)          | ComputeStopDate() — returns today for future        | ✅     |
| AC-013-2    | StopAllocationCommandHandler            | Lines 63–76 — compute and apply stop date           | ✅     |
| AC-013-3    | Frontend (out of scope for this review) | —                                                   | —      |
| AC-013-4    | StopAllocationCommandHandler            | Lines 79–85 — audit log                             | ✅     |
| AC-013-5    | StopAllocationCommandHandler            | Lines 53–58 — PM scope check                        | ✅     |
| AC-013-6    | StopAllocationCommandHandler            | Line 75 — constraint-safe guard                     | ✅     |
| AC-013-6    | StopAllocationCommandHandlerTests       | `Handle_FutureAllocation_ShouldSetToDateToFromDate` | ✅     |
| AC-013-6    | specs.md FR-013                         | New AC-013-6 acceptance criterion                   | ✅     |
| AC-013-6    | architecture.md §4.2                    | Handler note on constraint-safe logic               | ✅     |
| AC-013-6    | backend-test-report.md                  | Bug Fix #11 documented                              | ✅     |

### NFR-22 — User Identity Resolution

| Requirement | Artifact                         | Location                                  | Status |
| ----------- | -------------------------------- | ----------------------------------------- | ------ |
| NFR-22      | specs.md §9                      | NFR-22 definition                         | ✅     |
| NFR-22      | CurrentUserService.cs            | EmployeeId getter — multi-step resolution | ✅     |
| NFR-22      | architecture.md §4.3             | Identity Resolution Flow section          | ✅     |
| NFR-22      | api-spec.yaml info               | Identity resolution description           | ✅     |
| NFR-22      | specs.md §13 Integration/Auth    | empCode identity bridge documented        | ✅     |
| NFR-22      | specs.md §14 A-04                | empCode assumption added                  | ✅     |
| NFR-22      | backend-test-report.md           | Bug Fix #10 documented                    | ✅     |
| NFR-22      | architecture.md §5 Claim Mapping | empCode row in claim mapping table        | ✅     |

---

## 10. Verdict and Recommendations

### Verdict: **PASS WITH OBSERVATIONS**

Both bug fixes are correctly implemented, well-tested, and fully documented across all project artifacts. The code respects Clean Architecture boundaries, the security model is sound with JWT-backed claims, and the performance impact is acceptable for the expected load profile (≤100 concurrent users). All requirement traceability chains are complete.

### Recommended Follow-Up Actions

| Priority | Action                                                                                                                  | Finding |
| -------- | ----------------------------------------------------------------------------------------------------------------------- | ------- |
| Medium   | Replace synchronous DB calls in `CurrentUserService.EmployeeId` with an async resolution pattern (middleware or method) | CR-1    |
| Medium   | Fail fast (throw) when identity resolution returns `Guid.Empty` instead of silently proceeding                          | CR-2    |
| Low      | Move `ICurrentUserService` DI registration into `InfrastructureServiceExtensions.AddInfrastructure()`                   | CR-3    |
| Low      | Add unit tests for `CurrentUserService` covering all resolution paths                                                   | CR-4    |
| Info     | Add inline comment explaining dual `sub`/`ClaimTypes.NameIdentifier` lookup                                             | CR-5    |
| Info     | Consider a cross-document version matrix for release tracking                                                           | CR-6    |

---

_End of Code Review Report_
