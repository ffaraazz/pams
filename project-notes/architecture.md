# PAMS – System Architecture

---

## 1. Document Control

| Field          | Value                                       |
| -------------- | ------------------------------------------- |
| Project        | Project Allocation Management System (PAMS) |
| Version        | 1.5.0                                       |
| Date           | 2026-03-03                                  |
| Author         | ProductArchitect (GitHub Copilot)           |
| Status         | Approved for Implementation                 |
| Pipeline State | Phase 2 – Architecture Complete             |
| Input          | project-notes/specs.md v1.1.0               |

---

## 2. Technology Stack

| Layer           | Technology                                    | Version | Justification                                                                                 |
| --------------- | --------------------------------------------- | ------- | --------------------------------------------------------------------------------------------- |
| Runtime         | .NET                                          | 10.0    | LTS, latest; minimal APIs + Web API both supported                                            |
| Web Framework   | ASP.NET Core Web API                          | 10.0    | Built-in DI, middleware pipeline, OpenAPI support                                             |
| ORM             | Entity Framework Core                         | 10.0    | First-class .NET ORM; PostgreSQL provider available                                           |
| Database        | PostgreSQL                                    | 17.x    | JSONB available; robust date/range support; open source                                       |
| EF Provider     | Npgsql.EntityFrameworkCore.PostgreSQL         | 10.0    | Official Npgsql EF provider                                                                   |
| CQRS Mediator   | MediatR                                       | 12.x    | Mature; decouples command/query handlers from API layer                                       |
| Validation      | FluentValidation                              | 11.x    | Declarative, testable validation rules                                                        |
| Auth            | Microsoft.AspNetCore.Authentication.JwtBearer | 10.0    | Validates Keycloak-issued OIDC tokens via JWKS auto-discovery; PAMS is a resource server only |
| API Docs        | Scalar / Swashbuckle.AspNetCore               | latest  | OpenAPI 3.1 UI; replaces deprecated Swagger UI in .NET 10                                     |
| Mapping         | Mapster                                       | 7.x     | Faster than AutoMapper; source-gen friendly                                                   |
| Logging         | Serilog                                       | 4.x     | Structured logging; sinks: Console + File (+ future: Seq/ELK)                                 |
| Testing (Unit)  | xUnit                                         | 2.9.x   | Standard .NET test framework                                                                  |
| Testing (Int.)  | Testcontainers.PostgreSql                     | 3.x     | Spins real Postgres in Docker for integration tests                                           |
| Test Assertions | FluentAssertions                              | 7.x     | Readable, expressive assertions                                                               |
| Test Mocking    | NSubstitute                                   | 5.x     | Clean substitute syntax for interfaces                                                        |
| Migrations      | EF Core Migrations (CLI)                      | 10.0    | Code-first migrations versioned in source control                                             |
| Health Checks   | AspNetCore.HealthChecks.NpgSql                | 8.x     | Readiness/liveness probes for PostgreSQL                                                      |
| Rate Limiting   | ASP.NET Core built-in Rate Limiter            | 10.0    | No external dep; token-bucket policy                                                          |

---

## 3. Architecture Style

**Clean Architecture** (also called Onion / Ports-and-Adapters).

Dependency rule: outer layers depend inward. The **Domain** layer has zero external dependencies.

```
┌─────────────────────────────────────────────────────────────┐
│  PAMS.API  (Presentation)                                   │
│  Controllers · Middleware · Auth · Rate Limiting · Swagger  │
├─────────────────────────────────────────────────────────────┤
│  PAMS.Application  (Use Cases)                              │
│  Commands · Queries · DTOs · Validators · Interfaces        │
├─────────────────────────────────────────────────────────────┤
│  PAMS.Domain  (Core Business Rules)                         │
│  Entities · Enums · Domain Services · Repository Contracts  │
├─────────────────────────────────────────────────────────────┤
│  PAMS.Infrastructure  (Adapters)                            │
│  EF Core DbContext · Repositories · Migrations · Config     │
└─────────────────────────────────────────────────────────────┘
```

### Why Clean Architecture?

| Concern            | Benefit                                                                        |
| ------------------ | ------------------------------------------------------------------------------ |
| Testability        | Domain and Application layers have no EF/Postgres dependency → fast unit tests |
| Maintainability    | Swap ORM, database, or add messaging without touching business rules           |
| TDD readiness      | Repository interfaces mockable; validation logic purely in-process             |
| API contract first | Infrastructure details isolated; OpenAPI spec drives interface design          |

---

## 4. Layer Definitions

### 4.1 PAMS.Domain

Owns all business invariants. No NuGet dependencies (except `System` BCL).

**Contents:**

- `Entities/` – `Account`, `Project`, `Employee`, `Skill`, `EmployeeSkill`, `Allocation`, `SystemConfig`, `AuditLog`, `ProjectTeamMember`
- `Enums/` – `AccountType`, `EmployeeRole`, `ProjectStatus`, `AllocationStatus` (computed, not stored)
- `ValueObjects/` – `DateRange` (fromDate + toDate, encapsulates overlap logic), `AllocationPercentage`
- `DomainServices/`
  - `AllocationCapacityService` – core capacity check algorithm (FR-011); pure function; no I/O
  - `AllocationStopService` – stop-date computation rule (FR-013)
  - `ReportingChainValidator` – circular reporting detection (FR-007)
  - `TeamLeadValidator` – circular project-scoped reporting detection (FR-020); prevents A leads B, B leads A on same project
- `Exceptions/` – `DomainException`, `CapacityExceededException`, `CircularReportingException`, `UnauthorizedOperationException`
- `Repositories/` – `IAccountRepository`, `IProjectRepository`, `IEmployeeRepository`, `IAllocationRepository`, `ISkillRepository`, `ISystemConfigRepository`, `IProjectTeamMemberRepository`
- `Common/` – `IAuditableEntity`, `ISoftDeletable`, `IUnitOfWork`

**Key Domain Rule – Capacity Engine (FR-011):**

```
For employee E and date range [D1, D2]:
  For each active allocation A where A.employeeId = E
    and A.fromDate <= D2
    and (A.toDate IS NULL OR A.toDate >= D1):
      overlap_days = COUNT(days in [D1,D2] that fall within A)
      BUT since percentage is constant per allocation, we only need:
      max_daily_total = MAX over each day d in [D1,D2] of SUM(A.percentage)
                        where A overlaps d

  availableCapacity = 100 - max_daily_total
  If (existingTotal + newPercentage) > 100 → THROW CapacityExceededException
```

**Key Domain Rule – Stop Date (FR-013):**

```
if allocation.fromDate > TODAY: stopDate = TODAY           (cancel before start)
else:                            stopDate = TODAY + 1 day  (release from tomorrow)
```

**Key Domain Rule – Billable Default:**

```
On CreateProject:
  if (account.AccountType == Client)  → billable = request.billable ?? true
  else                                → billable = request.billable ?? false
```

HR can always override `billable` explicitly at creation time.

---

### 4.2 PAMS.Application

Orchestrates domain objects. Depends on **Domain** only (via interfaces). Contains all CQRS handlers.

**Contents:**

- `Commands/`
  - `Accounts/` – `CreateAccountCommand`, `UpdateAccountCommand` _(set isActive=false to deactivate)_
  - `Projects/` – `CreateProjectCommand`, `UpdateProjectCommand` _(set isActive=false to deactivate; HR or PM — PM scoped to own projects)_
  - `Employees/` – `CreateEmployeeCommand`, `UpdateEmployeeCommand` _(set isActive=false to deactivate)_
  - `Allocations/` – `CreateAllocationCommand`, `UpdateAllocationCommand`, `StopAllocationCommand` _(via PATCH)_, `RemoveAllocationCommand` _(via DELETE, soft-delete)_
  - `Skills/` – `CreateSkillCommand`, `UpdateSkillCommand`
  - `SystemConfig/` – `UpdateSystemConfigCommand`
  - `ProjectTeamMembers/` – `AddProjectTeamMemberCommand`, `RemoveProjectTeamMemberCommand`
  - `EmployeeSkills/` – `ManageOwnSkillsCommand` _(MVP2 — employee self-manage skills; handler rejects with 501 until MVP2 feature flag enabled)_
- `Queries/`
  - `Accounts/` – `GetAccountsQuery`, `GetAccountByCodeQuery`
  - `Projects/` – `GetProjectsQuery`, `GetProjectByCodeQuery`
  - `Employees/` – `GetEmployeesQuery` _(unified: search + list + filter via query params; no separate SearchEmployeesQuery)_, `GetEmployeeByCodeQuery`, `GetEmployeeAllocationStatusQuery`
  - `Allocations/` – `GetAllocationsByEmployeeQuery`, `GetAllocationsByProjectQuery`
  - `ProjectTeamMembers/` – `GetProjectTeamMembersQuery`
  - `Skills/` – `GetSkillsQuery`
  - `SystemConfig/` – `GetSystemConfigQuery`
- `DTOs/` – Response DTOs for each entity and compound views; named `*Response`, `*Summary`, `*Detail`
- `Validators/` – `CreateAllocationCommandValidator`, one per command
- `Interfaces/` – `ICurrentUserService`, `IDateTimeProvider`, `IAuditLogService`
- `Behaviors/` – `ValidationBehavior<TRequest,TResponse>` (MediatR pipeline behavior), `LoggingBehavior`, `TransactionBehavior`
- `Exceptions/` – `NotFoundException`, `ForbiddenException`, `ConflictException`
- `Mappings/` – Mapster configuration

**Command/Query segregation:**

- Commands: mutate state; wrapped in `TransactionBehavior` (DB transaction + audit log)
- Queries: read-only; bypass transaction behavior; may use raw SQL / Dapper for complex queries

> **StopAllocationCommandHandler constraint-safe stop date logic:** When domain service returns a stop date earlier than `allocation.FromDate` (cancelling a future allocation), the handler uses `FromDate` as `ToDate` to satisfy DB constraint `chk_allocation_dates` (`to_date >= from_date`).

---

### 4.3 PAMS.Infrastructure

Implements interfaces defined in Domain and Application. Depends on both.

**Contents:**

- `Persistence/`
  - `PamsDbContext.cs` – EF Core DbContext; all DbSets
  - `Configurations/` – `IEntityTypeConfiguration<T>` per entity (explicit column types, indexes, constraints); includes `ProjectTeamMemberConfiguration`
  - `Repositories/` – concrete repository implementations using `PamsDbContext`; includes `ProjectTeamMemberRepository`
  - `Migrations/` – EF Core migration files (never hand-edited after generation)
  - `UnitOfWork.cs`
  - `Seed/` – `SystemConfigSeeder`, `SkillSeeder` (invoked at startup in dev/staging)
- `Services/`
  - `CurrentUserService.cs` – reads JWT claims from `IHttpContextAccessor` and resolves authenticated user's Employee ID via `PamsDbContext` database lookup. Identity resolution strategy: (1) try `sub` claim → verify GUID exists in employees table, (2) fallback to `empCode` claim → find employee by EmpCode, (3) cache resolved ID per-request. Depends on `IHttpContextAccessor` and `PamsDbContext`.
  - `DateTimeProvider.cs` – wraps `DateOnly.FromDateTime(DateTime.UtcNow)` for testability
  - `AuditLogService.cs` – writes to `audit_logs` table; never throws to caller
- `Extensions/`
  - `InfrastructureServiceExtensions.cs` – registers all infrastructure services

**EF Core Configuration Notes:**

- All `DateOnly` columns mapped to PostgreSQL `date` type (no time component)
- `percentage` stored as `smallint`
- Soft-delete global query filter on `Allocation`: `modelBuilder.Entity<Allocation>().HasQueryFilter(a => a.DeletedAt == null)`
- `isActive` global query filter NOT applied by default (filtered per query as needed)
- Composite index: `(employee_id, from_date, to_date)` on `allocations`
- Partial index on `allocations`: `WHERE deleted_at IS NULL` for capacity computation
- `AuditLog` table append-only; no Repository pattern needed — `DbContext.Add()` directly from `AuditLogService`
- `ProjectTeamMember` table: composite unique constraint `(project_id, team_lead_id, reportee_id)`; check constraint `team_lead_id != reportee_id`; indexes on `project_id`, `team_lead_id`, `reportee_id`

### Identity Resolution Flow (Keycloak → PAMS)

1. JWT Bearer token arrives with claims: `sub` (Keycloak internal UUID), `empCode` (custom attribute), `realm_access.roles`
2. CurrentUserService.EmployeeId getter:
   a. Check `sub` claim → parse as GUID → query `employees` table by ID → if exists, use it
   b. Fallback: read `empCode` claim → query `employees` table by EmpCode → use resulting ID
   c. Cache resolved GUID for remainder of HTTP request (scoped lifetime)
3. CurrentUserService.Role: maps Keycloak realm roles (HR, ProjectManager) to EmployeeRole enum
4. All command handlers use ICurrentUserService.EmployeeId for:
   - PM scope enforcement (project ownership check)
   - Audit log `performedById` field
   - Allocation `allocatedById` foreign key

---

### 4.4 PAMS.API

Presentation layer. HTTP in; HTTP out. No business logic.

**Contents:**

- `Controllers/`
  - `AccountsController.cs`
  - `ProjectsController.cs`
  - `EmployeesController.cs`
  - `AllocationsController.cs`
  - `SkillsController.cs`
  - `SystemConfigController.cs`
  - `ProjectTeamMembersController.cs` – CRUD for project-scoped team lead / reportee mappings (FR-020)
- `Middleware/`
  - `ExceptionHandlerMiddleware.cs` – maps domain/application exceptions to RFC 9457 `ProblemDetails`
  - `RequestLoggingMiddleware.cs` – logs method, path, status, duration
- `Filters/` – `RequireRoleAttribute` (wraps `[Authorize(Roles = "...")]`)
- `Extensions/`
  - `ServiceCollectionExtensions.cs` – Swagger/Scalar, Auth, CORS, Rate Limiting, Health Checks
  - `ApplicationBuilderExtensions.cs` – middleware pipeline order
- `Models/` – `ProblemDetails` error codes constant class

---

## 5. Authentication & Authorization

### Auth Mechanism: Keycloak OAuth2 / OIDC

**Architecture:** PAMS is a **resource server only**. Keycloak is the Authorization Server and identity provider. PAMS never issues, stores, or validates passwords.

```
[ User / API Client ]
        │
        │  1. Obtain token via Keycloak Authorization Code or Client Credentials flow
        ▼
[ Keycloak  (Authorization Server) ]
        │  issues OIDC access token (RS256 signed)
        │
        │  2. Call PAMS API with  Authorization: Bearer <keycloak_token>
        ▼
[ PAMS.API  (Resource Server) ]
        │
        │  3. JwtBearerMiddleware fetches JWKS from Keycloak
        │     /.well-known/openid-configuration  (auto-discovery)
        │     Validates: signature · issuer · audience · expiry
        │
        │  4. Extract claims → ICurrentUserService
        │
        │  5. Enforce policy-based authorization
        ▼
[ Handler / Domain ]
```

**ASP.NET Core Configuration (in `ServiceCollectionExtensions.cs`):**

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["KeycloakSettings:Authority"];
        // e.g. https://keycloak.internal/realms/pams
        options.Audience  = builder.Configuration["KeycloakSettings:Audience"];
        // e.g. pams-api
        options.RequireHttpsMetadata = bool.Parse(
            builder.Configuration["KeycloakSettings:RequireHttpsMetadata"] ?? "true");
        // ASP.NET Core auto-discovers signing keys from
        // {Authority}/.well-known/openid-configuration
    });
```

**Keycloak Claim Mapping → `ICurrentUserService`:**
| OIDC Claim | Mapped To | Notes |
| --------------------------- | ---------------------------- | -------------------------------------------------- |
| `sub` | `CurrentUser.EmployeeId` | Keycloak user UUID; must match `employees.employee_id` on first login sync |
| `empCode` (custom attribute)| `CurrentUser.EmpCode` | Set as a user attribute in Keycloak realm |
| `realm_access.roles[]` | `CurrentUser.Role` | First matching value: `HR` / `ProjectManager` / `Staff` mapped to `EmployeeRole` enum |
| `name` | Display only | Full name from Keycloak profile |

**Authorization Policies (registered in DI — unchanged):**
| Policy Name | Roles | Used On |
| -------------------- | ------------------------- | ------------------------------------------------ |
| `HROnly` | HR | Account/Project/Employee CRUD |
| `CanAllocate` | HR, ProjectManager | Allocation create/update/stop/remove |
| `AuthenticatedUser` | HR, ProjectManager, Staff | Staff own-allocation view, profile |

**PM Scope Enforcement (server-side — unchanged):**

In `CreateAllocationCommandHandler`, `UpdateAllocationCommandHandler`, `StopAllocationCommandHandler`, `RemoveAllocationCommandHandler`:

1. Load the `Project` by `projectCode` (resolved to entity via repository).
2. If `currentUser.Role == ProjectManager` AND `project.ProjectManagerId != currentUser.EmployeeId` → throw `ForbiddenException("ERR_NOT_PROJECT_OWNER")`.
3. This check is in the Application layer — not bypassed by any HTTP manipulation.

**Future Migration Path — Azure AD / Entra ID:**
Swap `KeycloakSettings:Authority` to the Entra ID OIDC endpoint. The `ICurrentUserService` interface, all handlers, and all authorization policies remain unchanged. Only claim source names (`realm_access.roles` → `roles`) require a mapping update in `CurrentUserService`.

---

## 6. Data Flow

### 6.1 Create Allocation (Happy Path)

```
POST /allocations
    │
    ▼
[ExceptionHandlerMiddleware]
    │
    ▼
[JwtBearerMiddleware (Keycloak JWKS validation)] → validate token → inject ICurrentUserService
    │
    ▼
AllocationsController.Create(CreateAllocationRequest)
    │
    ▼ MediatR.Send(CreateAllocationCommand)
    │
    ▼ [ValidationBehavior] → FluentValidation rules
    │   (percentageIsMultipleOfIncrement, fromDate <= toDate, etc.)
    │
    ▼ [TransactionBehavior] → BEGIN TRANSACTION
    │
    ▼ CreateAllocationCommandHandler
    │   1. ICurrentUserService → get caller role + employeeId
    │   2. IProjectRepository.GetByCodeAsync(projectCode) → check active
    │   3. if PM: enforce project ownership
    │   4. IEmployeeRepository.GetByEmpCodeAsync(empCode) → check active
    │   5. IAllocationRepository.GetOverlappingAsync(employee.Id, fromDate, toDate)
    │   6. AllocationCapacityService.Validate(existing, newPct, range) → throws CapacityExceededException
    │   7. Build Allocation entity; set allocatedById = currentUser.EmployeeId
    │   8. IAllocationRepository.AddAsync(allocation)
    │   9. IUnitOfWork.SaveChangesAsync()
    │  10. IAuditLogService.LogAsync("allocation.created", payload)
    │
    ▼ [TransactionBehavior] → COMMIT
    │
    ▼ Map Allocation → AllocationDetailResponse
    │
    ▼ HTTP 201 Created  { allocationId, ... }
```

### 6.2 Concurrent Allocation Guard

Risk R-01 (race condition) is handled at the database level:

```sql
-- In GetOverlappingAsync, use SELECT ... FOR UPDATE on the employee's allocation rows
-- This serializes concurrent allocations for the same employee
-- EF: context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock({employeeId.GetHashCode()})")
```

Implementation: Use `PostgreSQL advisory locks` keyed on `employeeId` within the transaction scope. This prevents two simultaneous requests from both passing the capacity check for the same employee.

### 6.3 Capacity Check Algorithm (SQL-optimized)

For large datasets, the capacity check uses a window-function query instead of loading all entities into memory:

```sql
SELECT COALESCE(SUM(percentage), 0) AS total_allocated
FROM allocations
WHERE employee_id = @employeeId
  AND deleted_at IS NULL
  AND from_date <= @toDate
  AND (to_date IS NULL OR to_date >= @fromDate)
  -- For the edit case, exclude the allocation being modified:
  AND (allocation_id != @excludeAllocationId OR @excludeAllocationId IS NULL)
```

The query returns the maximum overlapping sum. If `total_allocated + @newPercentage > 100`, throw `CapacityExceededException`.

> **Note:** The simple SUM approach is safe because all overlapping allocations' percentages are summed for the worst-case day. This is correct per FR-011 requirements.

---

## 7. Database Design

### 7.1 Schema: `pams`

All tables live in the `pams` schema.

### 7.2 Tables

#### `accounts`

```sql
CREATE TABLE pams.accounts (
    account_id   UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    account_code VARCHAR(50)  NOT NULL UNIQUE,
    account_name VARCHAR(150) NOT NULL,
    account_type VARCHAR(20)  NOT NULL CHECK (account_type IN ('Client','Internal','Bench')),
    is_active    BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at   TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_accounts_code ON pams.accounts (LOWER(account_code));
```

#### `projects`

```sql
CREATE TABLE pams.projects (
    project_id          UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    project_code        VARCHAR(50)  NOT NULL UNIQUE,
    project_name        VARCHAR(150) NOT NULL,
    account_id          UUID         NOT NULL REFERENCES pams.accounts(account_id),
    project_manager_id  UUID         REFERENCES pams.employees(employee_id),
    start_date          DATE         NOT NULL,
    end_date            DATE,
    status              VARCHAR(20)  NOT NULL DEFAULT 'Upcoming'
                                     CHECK (status IN ('Upcoming','Active','Completed')),
    billable            BOOLEAN      NOT NULL DEFAULT TRUE,
    is_active           BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_project_dates CHECK (end_date IS NULL OR end_date >= start_date)
);
CREATE INDEX idx_projects_account ON pams.projects (account_id);
CREATE INDEX idx_projects_code ON pams.projects (LOWER(project_code));
```

#### `employees`

```sql
CREATE TABLE pams.employees (
    employee_id  UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    emp_code     VARCHAR(50)  NOT NULL UNIQUE,
    first_name   VARCHAR(100) NOT NULL,
    last_name    VARCHAR(100) NOT NULL,
    email        VARCHAR(254) NOT NULL UNIQUE,
    designation  VARCHAR(150) NOT NULL,
    role         VARCHAR(20)  NOT NULL CHECK (role IN ('HR','ProjectManager','Staff')),
    reports_to   UUID         REFERENCES pams.employees(employee_id),
    is_active    BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at   TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_employees_empcode ON pams.employees (LOWER(emp_code));
CREATE INDEX idx_employees_reportsto ON pams.employees (reports_to) WHERE reports_to IS NOT NULL;
CREATE INDEX idx_employees_fullname ON pams.employees (LOWER(first_name || ' ' || last_name));
```

#### `skills`

```sql
CREATE TABLE pams.skills (
    skill_id   UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    skill_name VARCHAR(100) NOT NULL UNIQUE,
    is_active  BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_skills_name ON pams.skills (LOWER(skill_name));
```

#### `employee_skills`

```sql
CREATE TABLE pams.employee_skills (
    employee_id UUID NOT NULL REFERENCES pams.employees(employee_id) ON DELETE CASCADE,
    skill_id    UUID NOT NULL REFERENCES pams.skills(skill_id),
    PRIMARY KEY (employee_id, skill_id)
);
```

#### `allocations`

```sql
CREATE TABLE pams.allocations (
    allocation_id   UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_id     UUID        NOT NULL REFERENCES pams.employees(employee_id),
    project_id      UUID        NOT NULL REFERENCES pams.projects(project_id),
    from_date       DATE        NOT NULL,
    to_date         DATE,
    percentage      SMALLINT    NOT NULL CHECK (percentage BETWEEN 1 AND 100),
    allocated_by_id UUID        NOT NULL REFERENCES pams.employees(employee_id),
    deleted_at      TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_allocation_dates CHECK (to_date IS NULL OR to_date >= from_date)
);
-- Primary capacity query index
CREATE INDEX idx_alloc_employee_dates ON pams.allocations (employee_id, from_date, to_date)
    WHERE deleted_at IS NULL;
-- Project view query index
CREATE INDEX idx_alloc_project ON pams.allocations (project_id)
    WHERE deleted_at IS NULL;
-- Employee view
CREATE INDEX idx_alloc_employee_active ON pams.allocations (employee_id)
    WHERE deleted_at IS NULL;
```

#### `system_configs`

```sql
CREATE TABLE pams.system_configs (
    config_key   VARCHAR(100) PRIMARY KEY,
    config_value VARCHAR(255) NOT NULL,
    updated_at   TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);
-- Seed:
INSERT INTO pams.system_configs VALUES ('minAllocationPct', '25', NOW());
INSERT INTO pams.system_configs VALUES ('allocationIncrement', '5', NOW());
```

#### `audit_logs`

```sql
CREATE TABLE pams.audit_logs (
    audit_id     UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    actor_id     UUID        NOT NULL,  -- employeeId of who performed the action
    action       VARCHAR(100) NOT NULL, -- e.g. 'allocation.created'
    entity_type  VARCHAR(100) NOT NULL,
    entity_id    UUID,
    payload      JSONB,                 -- before/after snapshot
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_auditlog_entity ON pams.audit_logs (entity_type, entity_id);
CREATE INDEX idx_auditlog_actor ON pams.audit_logs (actor_id);
CREATE INDEX idx_auditlog_created ON pams.audit_logs (created_at DESC);
```

#### `project_team_members`

```sql
CREATE TABLE pams.project_team_members (
    id             UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    project_id     UUID        NOT NULL REFERENCES pams.projects(project_id),
    team_lead_id   UUID        NOT NULL REFERENCES pams.employees(employee_id),
    reportee_id    UUID        NOT NULL REFERENCES pams.employees(employee_id),
    created_at     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_ptm_project_lead_reportee UNIQUE (project_id, team_lead_id, reportee_id),
    CONSTRAINT chk_ptm_lead_not_reportee CHECK (team_lead_id != reportee_id)
);
-- Project-based team config lookups
CREATE INDEX idx_ptm_project ON pams.project_team_members (project_id);
-- "Which projects does this employee lead?"
CREATE INDEX idx_ptm_team_lead ON pams.project_team_members (team_lead_id);
-- "On which projects is this employee a reportee?"
CREATE INDEX idx_ptm_reportee ON pams.project_team_members (reportee_id);
```

---

## 8. Repository Contracts

```csharp
// Domain/Repositories/IAccountRepository.cs
public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Account?> GetByCodeAsync(string accountCode, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string accountCode, CancellationToken ct = default);
    Task AddAsync(Account account, CancellationToken ct = default);
    void Update(Account account);
}
```

```csharp
// Domain/Repositories/IProjectRepository.cs
public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Project?> GetByCodeAsync(string projectCode, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string projectCode, CancellationToken ct = default);
    Task AddAsync(Project project, CancellationToken ct = default);
    void Update(Project project);
}
```

```csharp
// Domain/Repositories/IAllocationRepository.cs
public interface IAllocationRepository
{
    Task<Allocation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Allocation>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default);
    Task<IReadOnlyList<Allocation>> GetByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<int> GetOverlappingTotalPercentageAsync(
        Guid employeeId, DateOnly from, DateOnly? to,
        Guid? excludeAllocationId = null, CancellationToken ct = default);
    Task AddAsync(Allocation allocation, CancellationToken ct = default);
    void Update(Allocation allocation);
    void SoftDelete(Allocation allocation);
}
```

```csharp
// Domain/Repositories/IEmployeeRepository.cs
public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Employee?> GetByEmpCodeAsync(string empCode, CancellationToken ct = default);
    Task<Employee?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<Employee>> GetFilteredAsync(
        string? search, Guid? skillId, bool benchOnly, string? role,
        bool? isActive, DateOnly? windowFrom, DateOnly? windowTo,
        int page, int limit, CancellationToken ct = default);
    Task<int> GetFilteredCountAsync(
        string? search, Guid? skillId, bool benchOnly, string? role,
        bool? isActive, DateOnly? windowFrom, DateOnly? windowTo,
        CancellationToken ct = default);
    Task<bool> WouldCreateCircularReportingAsync(Guid employeeId, Guid reportsToId, CancellationToken ct = default);
    Task AddAsync(Employee employee, CancellationToken ct = default);
    void Update(Employee employee);
}
```

```csharp
// Domain/Repositories/IProjectTeamMemberRepository.cs
public interface IProjectTeamMemberRepository
{
    Task<IReadOnlyList<ProjectTeamMember>> GetByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<IReadOnlyList<ProjectTeamMember>> GetByTeamLeadAsync(Guid teamLeadId, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid projectId, Guid teamLeadId, Guid reporteeId, CancellationToken ct = default);
    Task<bool> WouldCreateCircularLeadershipAsync(Guid projectId, Guid teamLeadId, Guid reporteeId, CancellationToken ct = default);
    Task AddAsync(ProjectTeamMember member, CancellationToken ct = default);
    Task RemoveAsync(Guid projectId, Guid teamLeadId, Guid reporteeId, CancellationToken ct = default);
}
```

---

## 9. Caching Strategy

**MVP: No distributed cache.** Simple in-process memory caching only.

| Data                  | Cache Strategy                 | TTL    | Eviction                       |
| --------------------- | ------------------------------ | ------ | ------------------------------ |
| `SystemConfig` values | `IMemoryCache` on startup load | 5 min  | On `UpdateSystemConfigCommand` |
| Skill list            | `IMemoryCache`                 | 10 min | On skill CRUD                  |

No Redis in MVP. If read load grows, introduce `IDistributedCache` backed by Redis as a post-MVP improvement with no interface changes.

---

## 10. Error Handling & Problem Details

All exceptions are mapped in `ExceptionHandlerMiddleware` to RFC 9457 `ProblemDetails`:

| Exception                                | HTTP Status | Error Code                                |
| ---------------------------------------- | ----------- | ----------------------------------------- |
| `NotFoundException`                      | 404         | `ERR_NOT_FOUND`                           |
| `ForbiddenException`                     | 403         | `ERR_FORBIDDEN` / `ERR_NOT_PROJECT_OWNER` |
| `ConflictException`                      | 409         | `ERR_ACCOUNT_CODE_EXISTS` etc.            |
| `CapacityExceededException`              | 422         | `ERR_CAPACITY_EXCEEDED`                   |
| `CircularReportingException`             | 422         | `ERR_CIRCULAR_REPORTING`                  |
| `ValidationException` (FluentValidation) | 400         | `ERR_VALIDATION`                          |
| `UnauthorizedAccessException`            | 401         | `ERR_UNAUTHORIZED`                        |
| `DomainException` (base)                 | 422         | domain-specific code                      |
| Unhandled exception                      | 500         | `ERR_INTERNAL`                            |

**ProblemDetails shape:**

```json
{
  "type": "https://pams.internal/errors/ERR_CAPACITY_EXCEEDED",
  "title": "Capacity Exceeded",
  "status": 422,
  "detail": "Employee EMP042 is already 75% allocated on 2026-03-01. Available: 25%.",
  "instance": "/allocations",
  "traceId": "00-abc123-def456-00",
  "extensions": {
    "employeeId": "...",
    "conflictDate": "2026-03-01",
    "currentTotal": 75,
    "requested": 50,
    "available": 25
  }
}
```

---

## 11. Logging Strategy

**Library:** Serilog with structured properties.

**Enrichers:** `WithCorrelationId`, `WithMachineName`, `WithEnvironmentName`, `WithProperty("Service", "PAMS")`

**Sinks (MVP):**

- Console (JSON format for container compatibility)
- Rolling file: `logs/pams-.log` (daily)

**Log Levels by Namespace:**
| Namespace | Level |
| ------------------------ | ------- |
| `PAMS.API` | Info |
| `PAMS.Application` | Info |
| `PAMS.Infrastructure` | Warning |
| `Microsoft.EntityFrameworkCore.Database.Command` | Warning (suppress SQL in prod) |
| Unhandled exceptions | Error |

**Mandatory log events:**

- `[INFO]  HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms`
- `[INFO]  {Action} executed by {ActorEmpCode} on {EntityType}/{EntityId}`
- `[WARN]  Allocation capacity check: employee={EmpCode} requested={Pct}% available={Available}%`
- `[ERROR] Unhandled exception {ExceptionType}: {Message} | TraceId={TraceId}`

---

## 12. Health Checks

Endpoints registered at `/health`:
| Check Name | Type | Description |
| --------------- | --------------- | ------------------------------------- |
| `postgres` | Database | Executes `SELECT 1` on `PamsDbContext`|
| `self` | Always healthy | API process is alive |

Separate `/health/ready` (db + dependencies) and `/health/live` (self only) for Kubernetes probes.

---

## 13. API Versioning

**Strategy:** URL path versioning.

- All routes prefixed: `/api/v1/...`
- Version introduced in URL not headers (simpler for MVP; API consumers are internal tools/consumers)
- When breaking changes arise, `/api/v2/...` introduced; v1 deprecated with sunset date in response headers.

---

## 14. Rate Limiting

ASP.NET Core built-in `RateLimiter` (no external dep):
| Policy Name | Algorithm | Limit | Window | Applied To |
| -------------- | ------------ | ----------------- | -------- | -------------------- |
| `global` | Token Bucket | 300 req/user/min | 1 min | All authenticated |
| `search` | Sliding Window | 60 req/user/min | 1 min | `GET /employees` (when `search` param present) |

---

## 15. Cross-Cutting Concerns Summary

| Concern            | Implementation                                                                | Layer                      |
| ------------------ | ----------------------------------------------------------------------------- | -------------------------- |
| Auth/AuthZ         | Keycloak OAuth2/OIDC (JwtBearer JWKS validation) + Policy-based authorization | API                        |
| Validation         | FluentValidation via MediatR pipeline                                         | Application                |
| DB Transaction     | `TransactionBehavior` (MediatR)                                               | Application                |
| Exception Mapping  | `ExceptionHandlerMiddleware`                                                  | API                        |
| Audit Logging      | `AuditLogService` in `TransactionBehavior`                                    | Application/Infrastructure |
| Structured Logging | Serilog `RequestLoggingMiddleware`                                            | API                        |
| Soft Delete Filter | EF global query filter on `Allocation`                                        | Infrastructure             |
| SystemConfig Cache | `IMemoryCache` invalidated on mutation                                        | Infrastructure             |
| Concurrency Guard  | PostgreSQL advisory lock in `IAllocationRepository`                           | Infrastructure             |
| Circular Reporting | Graph traversal in `ReportingChainValidator`                                  | Domain                     |
| Team Lead Scope    | Project-scoped circular lead detection in `TeamLeadValidator`                 | Domain                     |

---

## 16. Enriched Response Strategy

### Enriched Response Strategy

As of v1.7.0, resource APIs embed related data directly in their responses. Dashboard aggregation endpoints have been removed (v1.8.0):

| Endpoint                   | Enrichment                                                                           | Purpose                                                                                  |
| -------------------------- | ------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------- |
| `GET /projects/{code}`     | `+allocations[]`, `+teamMembers[]`                                                   | Single call for full project view with resources and team assignments                    |
| `GET /employees/me`        | `+managedProjects[]`                                                                 | Self-profile includes projects where user is PM or TeamLead                              |
| `GET /employees/{empCode}` | `+managedProjects[]`                                                                 | Employee detail includes management responsibilities                                     |
| `AllocationDetailResponse` | `+designation`, `+billable`, `+accountCode`, `+accountName`, `+status`, `+updatedAt` | Allocation responses carry denormalized display data from Employee, Project, and Account |

This approach follows the **Backend-for-Frontend (BFF) pattern** — the API shapes responses to match what the consumer needs in a single call, reducing round-trips and client-side data joining.

### Dashboard Removal

`DashboardController` and its handlers (`GetProjectViewDashboardQuery`, `GetEmployeeViewDashboardQuery`) have been removed from the codebase in v1.8.0:

- `GET /api/v1/dashboard/project-view` → removed; use enriched `GET /projects/{code}`
- `GET /api/v1/dashboard/employee-view` → removed; use `GET /employees` + `GET /employees/me`
- `CanViewDashboard` authorization policy → removed

---

## 17. Architectural Decisions & Tradeoffs

| Decision                  | Chosen Approach                                | Rejected Alternative                 | Reason                                                                                                                              |
| ------------------------- | ---------------------------------------------- | ------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------- |
| Auth mechanism            | Keycloak OAuth2/OIDC (PAMS as resource server) | Local JWT + password store           | Delegates identity entirely to Keycloak; no password management in PAMS; future swap to Azure AD requires only Authority URL change |
| CQRS implementation       | MediatR in-process                             | Event sourcing                       | PAMS is CRUD-heavy; full ES is premature                                                                                            |
| ORM                       | EF Core (code-first)                           | Dapper                               | Migration management; LINQ for complex queries; Dapper used for perf-critical queries                                               |
| Soft delete               | `deletedAt` timestamp                          | Status enum + hard delete            | Audit requirement; history preservation                                                                                             |
| Allocation capacity query | SQL SUM in one query                           | Load all allocations to memory       | Performance (≤300ms per FR-011 AC-011-5)                                                                                            |
| Concurrency control       | Advisory locks                                 | Optimistic concurrency (row version) | Advisory locks guarantee serializability without retry logic                                                                        |
| API versioning            | URL path prefix                                | Header versioning                    | Simpler; API consumers are internal                                                                                                 |
| Caching                   | IMemoryCache (in-process)                      | Redis                                | Single-instance MVP; Redis adds ops overhead                                                                                        |
| Mapping                   | Mapster                                        | AutoMapper                           | Better performance; source-gen compatible                                                                                           |

---

## 17. Security Checklist

- [x] All endpoints require `[Authorize]` by default; no public endpoints (Keycloak manages the unauthenticated flow)
- [x] PM project-ownership check in Application layer (not just controller)
- [x] No passwords stored in PAMS; authentication fully delegated to Keycloak
- [x] Keycloak token signature verified via JWKS auto-discovery; no private keys in PAMS source control
- [x] `Content-Security-Policy`, `X-Content-Type-Options`, `X-Frame-Options` headers on all responses
- [x] Rate limiting on auth and search endpoints
- [x] Parameterized queries only (EF Core + raw SQL with parameters)
- [x] HTTPS enforced; HTTP redirected
- [x] `detailed_errors: false` in production (problem details omit stack traces)
- [x] Audit log for all mutations

---

## 18. Deployment Topology (MVP)

```
[Consumer / API Client]
        │  HTTPS
        ▼
[ Reverse Proxy / API Gateway (Nginx / YARP) ]
        │
        ▼
[ PAMS.API  (.NET 10 Web API) ]  ─── [ IMemoryCache ]
        │
        │  Npgsql
        ▼
[ PostgreSQL 17 ]
        │
        └── Schema: pams
            ├── accounts
            ├── projects
            ├── employees
            ├── employee_skills
            ├── skills
            ├── allocations
            ├── project_team_members
            ├── system_configs
            └── audit_logs
```

Deployment can be Docker Compose (development) or Kubernetes (production). Both are containerizable; the API image is a single `mcr.microsoft.com/dotnet/aspnet:10.0` container.
