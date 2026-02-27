# Backend Test Report

**Generated**: Phase 4 — BackendDeveloper  
**Date**: 2025-07-17 (updated)  
**Solution**: PAMS.slnx (.NET 10.0)  
**Test Framework**: xUnit 2.9.3, FluentAssertions 7.0.0, NSubstitute 5.3.0

---

## Summary

| Metric         | Value |
| -------------- | ----- |
| Total Tests    | 100   |
| Passed         | 100   |
| Failed         | 0     |
| Skipped        | 0     |
| Build Warnings | 0     |
| Build Errors   | 0     |

---

## Test Coverage by Feature Requirement

### Domain Layer (28 tests)

| Test File                      | FR-ID  | Tests | Status      |
| ------------------------------ | ------ | ----- | ----------- |
| AllocationCapacityServiceTests | FR-011 | 8     | ✅ All pass |
| AllocationStopServiceTests     | FR-013 | 6     | ✅ All pass |
| ReportingChainValidatorTests   | FR-007 | 6     | ✅ All pass |
| TeamLeadValidatorTests         | FR-020 | 8     | ✅ All pass |

### Application Layer — Handlers (19 tests)

| Test File                           | FR-ID  | Tests | Status      |
| ----------------------------------- | ------ | ----- | ----------- |
| CreateAllocationCommandHandlerTests | FR-010 | 7     | ✅ All pass |
| StopAllocationCommandHandlerTests   | FR-013 | 5     | ✅ All pass |
| RemoveAllocationCommandHandlerTests | FR-014 | 7     | ✅ All pass |

### Application Layer — Validators (32 tests)

| Test File                                 | FR-ID  | Tests | Status      |
| ----------------------------------------- | ------ | ----- | ----------- |
| CreateAllocationCommandValidatorTests     | FR-010 | 11    | ✅ All pass |
| CreateEmployeeCommandValidatorTests       | FR-007 | 15    | ✅ All pass |
| AddProjectTeamMemberCommandValidatorTests | FR-020 | 6     | ✅ All pass |

### Parameterized Test Breakdown

| Test                                                                | Inline Data Count |
| ------------------------------------------------------------------- | ----------------- |
| Validate_VariousExistingTotals_ShouldEnforceCapacityCorrectly       | 8 cases           |
| Validate_PercentageBelowMinimum_ShouldHaveValidationError           | 5 cases           |
| Validate_PercentageNotMultipleOfIncrement_ShouldHaveValidationError | 4 cases           |
| Validate_ValidPercentage_ShouldNotHaveValidationError               | 5 cases           |
| Validate_MissingFirstName_ShouldHaveValidationError                 | 2 cases           |
| Validate_InvalidEmailFormat_ShouldHaveValidationError               | 4 cases           |
| Validate_ValidRole_ShouldNotHaveValidationError                     | 3 cases           |

---

## Implementation Coverage

### Domain Layer (src/PAMS.Domain/) — COMPLETE

| Category     | Files                                                                                                                                                       |
| ------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Enums        | EmployeeRole, AccountType, ProjectStatus, AllocationStatus                                                                                                  |
| Common       | IAuditableEntity, ISoftDeletable, IUnitOfWork                                                                                                               |
| Exceptions   | DomainException, CapacityExceededException, CircularReportingException, UnauthorizedOperationException                                                      |
| Entities     | Account, Project, Employee, Allocation, Skill, EmployeeSkill, SystemConfig, AuditLog, ProjectTeamMember                                                     |
| Repositories | IAccountRepository, IProjectRepository, IAllocationRepository, IEmployeeRepository, ISkillRepository, ISystemConfigRepository, IProjectTeamMemberRepository |
| Services     | AllocationCapacityService, AllocationStopService, ReportingChainValidator, TeamLeadValidator                                                                |

### Application Layer (src/PAMS.Application/) — COMPLETE

| Category   | Files                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| ---------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Interfaces | ICurrentUserService, IAuditLogService, IDateTimeProvider                                                                                                                                                                                                                                                                                                                                                                                                                        |
| Exceptions | NotFoundException, ForbiddenException, ConflictException                                                                                                                                                                                                                                                                                                                                                                                                                        |
| Commands   | CreateAllocationCommand, StopAllocationCommand, RemoveAllocationCommand, UpdateAllocationCommand, CreateEmployeeCommand, UpdateEmployeeCommand, AddProjectTeamMemberCommand, RemoveProjectTeamMemberCommand, CreateAccountCommand, UpdateAccountCommand, CreateProjectCommand, UpdateProjectCommand, CreateSkillCommand, UpdateSkillCommand, UpdateSystemConfigCommand                                                                                                          |
| Handlers   | CreateAllocationCommandHandler, StopAllocationCommandHandler, RemoveAllocationCommandHandler, UpdateAllocationCommandHandler, CreateEmployeeCommandHandler, UpdateEmployeeCommandHandler, AddProjectTeamMemberCommandHandler, RemoveProjectTeamMemberCommandHandler, CreateAccountCommandHandler, UpdateAccountCommandHandler, CreateProjectCommandHandler, UpdateProjectCommandHandler, CreateSkillCommandHandler, UpdateSkillCommandHandler, UpdateSystemConfigCommandHandler |
| DTOs       | AllocationDetailResponse, CapacityCheckResponse, AccountSummaryResponse, AccountDetailResponse, ProjectSummaryResponse, ProjectDetailResponse, EmployeeSummaryResponse, EmployeeDetailResponse, SkillResponse, SystemConfigResponse, ProjectTeamMemberResponse, DashboardResponses, PagedResponse\<T\>                                                                                                                                                                          |
| Validators | CreateAllocationCommandValidator, CreateEmployeeCommandValidator, AddProjectTeamMemberCommandValidator                                                                                                                                                                                                                                                                                                                                                                          |

### Infrastructure Layer (src/PAMS.Infrastructure/) — COMPLETE

| Category       | Files                                                                                                                                                                                                                        |
| -------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| DbContext      | PamsDbContext (pams schema, global soft-delete filter)                                                                                                                                                                       |
| UnitOfWork     | UnitOfWork                                                                                                                                                                                                                   |
| Configurations | AccountConfiguration, ProjectConfiguration, EmployeeConfiguration, AllocationConfiguration, SkillConfiguration, EmployeeSkillConfiguration, SystemConfigConfiguration, AuditLogConfiguration, ProjectTeamMemberConfiguration |
| Repositories   | AccountRepository, ProjectRepository, EmployeeRepository, AllocationRepository, SkillRepository, SystemConfigRepository, ProjectTeamMemberRepository                                                                         |
| Services       | CurrentUserService, DateTimeProvider, AuditLogService                                                                                                                                                                        |
| Seeders        | SystemConfigSeeder, SkillSeeder                                                                                                                                                                                              |
| Migrations     | InitialCreate                                                                                                                                                                                                                |
| DI             | InfrastructureServiceExtensions                                                                                                                                                                                              |

### API Layer (src/PAMS.API/) — COMPLETE

| Category    | Files                                                                                                                                                                           |
| ----------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Program.cs  | Serilog, DI, MediatR, FluentValidation, auto-migrate, seeding, Scalar docs, health checks                                                                                       |
| Middleware  | ExceptionHandlerMiddleware (RFC 9457 ProblemDetails), RequestLoggingMiddleware                                                                                                  |
| Extensions  | ServiceCollectionExtensions (Auth, Policies, CORS, Health), ApplicationBuilderExtensions                                                                                        |
| Controllers | AccountsController, ProjectsController, EmployeesController, AllocationsController, SkillsController, SystemConfigController, ProjectTeamMembersController, DashboardController |

---

## API Endpoints Implemented (29 total)

| Method | Path                                                   | Auth Policy      | Controller                   |
| ------ | ------------------------------------------------------ | ---------------- | ---------------------------- |
| GET    | /api/v1/accounts                                       | CanAllocate      | AccountsController           |
| GET    | /api/v1/accounts/{accountCode}                         | CanAllocate      | AccountsController           |
| POST   | /api/v1/accounts                                       | HROnly           | AccountsController           |
| PUT    | /api/v1/accounts/{accountCode}                         | HROnly           | AccountsController           |
| GET    | /api/v1/projects                                       | Authenticated    | ProjectsController           |
| GET    | /api/v1/projects/{projectCode}                         | Authenticated    | ProjectsController           |
| POST   | /api/v1/projects                                       | HROnly           | ProjectsController           |
| PUT    | /api/v1/projects/{projectCode}                         | CanAllocate      | ProjectsController           |
| GET    | /api/v1/employees                                      | CanAllocate      | EmployeesController          |
| GET    | /api/v1/employees/{empCode}                            | Authenticated    | EmployeesController          |
| GET    | /api/v1/employees/me                                   | Authenticated    | EmployeesController          |
| POST   | /api/v1/employees                                      | HROnly           | EmployeesController          |
| PUT    | /api/v1/employees/{empCode}                            | HROnly           | EmployeesController          |
| POST   | /api/v1/allocations                                    | CanAllocate      | AllocationsController        |
| GET    | /api/v1/allocations/{id}                               | Authenticated    | AllocationsController        |
| PUT    | /api/v1/allocations/{id}                               | CanAllocate      | AllocationsController        |
| PATCH  | /api/v1/allocations/{id}                               | CanAllocate      | AllocationsController        |
| DELETE | /api/v1/allocations/{id}                               | CanAllocate      | AllocationsController        |
| GET    | /api/v1/allocations/capacity-check                     | CanAllocate      | AllocationsController        |
| GET    | /api/v1/skills                                         | Authenticated    | SkillsController             |
| POST   | /api/v1/skills                                         | HROnly           | SkillsController             |
| PUT    | /api/v1/skills/{id}                                    | HROnly           | SkillsController             |
| GET    | /api/v1/system-config                                  | HROnly           | SystemConfigController       |
| PUT    | /api/v1/system-config                                  | HROnly           | SystemConfigController       |
| GET    | /api/v1/projects/{code}/team-members                   | CanAllocate      | ProjectTeamMembersController |
| POST   | /api/v1/projects/{code}/team-members                   | CanAllocate      | ProjectTeamMembersController |
| DELETE | /api/v1/projects/{code}/team-members/{lead}/{reportee} | CanAllocate      | ProjectTeamMembersController |
| GET    | /api/v1/dashboard/project-view                         | CanViewDashboard | DashboardController          |
| GET    | /api/v1/dashboard/employee-view                        | CanViewDashboard | DashboardController          |

---

## Issues Found & Resolved During TDD

### 1. Test Compilation Error — FluentAssertions `.Or` Syntax

- **File**: AllocationCapacityServiceTests.cs (line 175)
- **Issue**: `.Or.Contain()` does not exist on `AndConstraint<StringAssertions>` in FluentAssertions 7.0
- **Fix**: Changed to `.MatchRegex("80|30|20")` preserving the OR intent
- **Classification**: Test syntax defect (logically incorrect usage)

### 2. DateTime.UtcNow vs DateTime.Today Mismatch

- **Files**: RemoveAllocationCommandHandler.cs, StopAllocationCommandHandler.cs
- **Issue**: Handlers used `DateOnly.FromDateTime(DateTime.UtcNow)` while TestData uses `DateOnly.FromDateTime(DateTime.Today)` (local time). In UTC+ timezones during early hours, UTC date is one day behind local, causing false comparison failures.
- **Fix**: Changed to `DateOnly.FromDateTime(DateTime.Today)` to match test expectations

### 3. Email Validation — FluentValidation `.EmailAddress()` Leniency

- **File**: CreateEmployeeCommandValidator.cs
- **Issue**: FluentValidation's default `EmailAddress()` (AspNetCoreCompatible mode) accepts `"spaces in@mail.com"`
- **Fix**: Replaced with `.Matches(@"^[^\s@]+@[^\s@]+\.[^\s@]+$")` for stricter validation

### 4. Entity Properties — NSubstitute Virtual Requirement

- **Files**: Account.cs, Project.cs, Employee.cs, Allocation.cs
- **Issue**: NSubstitute `Substitute.For<Entity>()` requires `virtual` properties to intercept `.Returns()` calls
- **Fix**: Made all entity properties `virtual`

### 5. SystemConfig Entity Modeling

- **File**: SystemConfig.cs
- **Issue**: Tests mock `config.MinAllocationPercentage.Returns(25)` and `config.AllocationIncrement.Returns(5)` — requires aggregate model with virtual properties, not key-value pairs
- **Fix**: Changed from key-value model to aggregate model with `virtual int MinAllocationPercentage` and `virtual int AllocationIncrement`

### 6. Handler Capacity Check — GetOverlappingAsync

- **File**: CreateAllocationCommandHandler.cs
- **Issue**: Tests mock `GetOverlappingAsync(...)` returning `List<Allocation>`, not `GetOverlappingTotalPercentageAsync` returning `int`
- **Fix**: Handler fetches overlapping allocations via `GetOverlappingAsync`, then sums percentages with `overlapping.Sum(a => a.Percentage)`

---

## Test Coverage Gaps

New command handlers created during controller implementation phase do not yet have unit tests. The following handlers need test coverage from TestEngineer:

| Handler                               | FR-ID  | Priority |
| ------------------------------------- | ------ | -------- |
| CreateAccountCommandHandler           | FR-001 | High     |
| UpdateAccountCommandHandler           | FR-002 | High     |
| CreateProjectCommandHandler           | FR-003 | High     |
| UpdateProjectCommandHandler           | FR-004 | High     |
| CreateEmployeeCommandHandler          | FR-007 | High     |
| UpdateEmployeeCommandHandler          | FR-008 | High     |
| UpdateAllocationCommandHandler        | FR-012 | High     |
| CreateSkillCommandHandler             | FR-015 | Medium   |
| UpdateSkillCommandHandler             | FR-016 | Medium   |
| UpdateSystemConfigCommandHandler      | FR-017 | Medium   |
| AddProjectTeamMemberCommandHandler    | FR-020 | High     |
| RemoveProjectTeamMemberCommandHandler | FR-021 | Medium   |

---

## Layer Completion Status

| Layer             | Status      | Notes                                                               |
| ----------------- | ----------- | ------------------------------------------------------------------- |
| Domain            | ✅ Complete | All entities, services, repository interfaces                       |
| Application       | ✅ Complete | 15 commands, 15 handlers, 3 validators, 13 DTO types                |
| Infrastructure    | ✅ Complete | DbContext, 9 configs, 7 repos, 3 services, 2 seeders, migration     |
| API               | ✅ Complete | 8 controllers, 29 endpoints, middleware, auth, Program.cs           |
| Unit Tests        | ✅ Complete | 100/100 pass (covers original 3 handlers + 3 validators + 4 domain) |
| Integration Tests | ⬜ Pending  | Testcontainers.PostgreSql setup needed                              |

---

## Notes

1. **MVP2 endpoints not yet implemented:** Employee self-manage skills (`/employees/me/skills`) endpoints are MVP2 scope.
2. **Keycloak required:** Auth requires running Keycloak instance with PAMS realm, client, and roles (HR, ProjectManager, Staff).
3. **Running the app:** `docker-compose up -d` → `dotnet run --project src/PAMS.API` → Scalar docs at `/scalar/v1`.
