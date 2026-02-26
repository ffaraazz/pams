# PAMS – Development Best Practices

---

## 1. Document Control

| Field   | Value                                       |
| ------- | ------------------------------------------- |
| Project | Project Allocation Management System (PAMS) |
| Version | 1.4.0                                       |
| Date    | 2026-02-26                                  |
| Author  | ProductArchitect (GitHub Copilot)           |
| Status  | Approved for Implementation                 |

---

## 2. Code Organization

### 2.1 One Concern, One File

Each file contains exactly one type (class, interface, enum, record). No partial files except for EF `OnModelCreating` split by convention.

### 2.2 Feature Folders Over Type Folders

In the Application layer, group by **feature** (e.g., `Commands/Allocations/CreateAllocation/`) not by type (all handlers in one folder).

### 2.3 Dependency Direction

```
API → Application → Domain ← Infrastructure
```

- **Domain** has zero framework dependencies.
- **Application** depends only on Domain.
- **Infrastructure** implements Domain + Application interfaces.
- **API** wires everything together; never imports `PAMS.Domain` directly unless for enum/exception types.

### 2.4 No Business Logic in Controllers

Controllers are thin:

```csharp
// CORRECT
[HttpPost]
public async Task<IActionResult> Create(CreateAllocationRequest request, CancellationToken ct)
{
    var command = request.Adapt<CreateAllocationCommand>();
    var result = await _mediator.Send(command, ct);
    return CreatedAtAction(nameof(GetById), new { allocationId = result.AllocationId }, result);
}

// WRONG – business logic in controller
[HttpPost]
public async Task<IActionResult> Create(CreateAllocationRequest request)
{
    if (request.Percentage < 25) return BadRequest("Too low"); // business rule leaking into controller
    ...
}
```

---

## 3. Naming Conventions

### C# Code

| Symbol         | Convention                       | Example                                         |
| -------------- | -------------------------------- | ----------------------------------------------- |
| Interface      | `I` prefix                       | `IAllocationRepository`                         |
| Abstract class | no prefix                        | `DomainException`                               |
| Private field  | `_camelCase`                     | `_mediator`, `_dbContext`                       |
| Property       | PascalCase                       | `FromDate`, `EmployeeId`                        |
| Method         | PascalCase verb                  | `GetOverlappingTotalPercentageAsync`            |
| Async method   | `Async` suffix                   | `GetByCodeAsync`, `SaveChangesAsync`            |
| Constant       | PascalCase                       | `MaxAllocationPercentage`                       |
| Record DTO     | PascalCase                       | `AllocationDetailResponse`                      |
| Enum member    | PascalCase                       | `EmployeeRole.ProjectManager`                   |
| Test method    | `Method_Scenario_ExpectedResult` | `Validate_WhenCapacityExceeded_ThrowsException` |

### Database

| Symbol     | Convention                | Example                    |
| ---------- | ------------------------- | -------------------------- |
| Table      | `snake_case` plural       | `employee_skills`          |
| Column     | `snake_case`              | `emp_code`, `from_date`    |
| PK         | `<table_singular>_id`     | `allocation_id`            |
| FK         | `<ref_table_singular>_id` | `employee_id`              |
| Index      | `idx_<table>_<cols>`      | `idx_alloc_employee_dates` |
| Constraint | `chk_<table>_<purpose>`   | `chk_allocation_dates`     |
| Schema     | lowercase                 | `pams`                     |

---

## 4. Entity Design (Domain Layer)

### 4.1 Entities Are Not Anemic

Entities contain behavior that enforces their own invariants:

```csharp
// CORRECT – entity owns its stop logic
public class Allocation
{
    public DateOnly FromDate { get; private set; }
    public DateOnly? ToDate { get; private set; }

    public void Stop(DateOnly today)
    {
        if (ToDate.HasValue && ToDate.Value <= today)
            throw new DomainException("Allocation is already ended.");

        ToDate = FromDate > today ? today : today.AddDays(1);
        UpdatedAt = DateTime.UtcNow;
    }
}

// WRONG – stop logic in handler
allocation.ToDate = today.AddDays(1); // bypasses invariants
```

### 4.2 Private Setters

All entity properties use `private set` or `init`. Mutation only via public methods with clear intent.

### 4.3 No Public Parameterless Constructor on Entities

Use a static factory method for creation:

```csharp
public static Allocation Create(
    Guid employeeId, Guid projectId,
    DateOnly from, DateOnly? to, int percentage, Guid allocatedById)
{
    // validate and construct
}
```

EF Core can use the private parameterless constructor for materialization (EF convention).

### 4.4 Enums for Fixed-Value Types

Never use raw strings for role, account type, project status in domain code. Always use enums. EF stores them as `VARCHAR` columns via value converters.

---

## 5. Command / Query Design (Application Layer)

### 5.1 Commands Are Records

```csharp
public sealed record CreateAllocationCommand(
    string EmpCode,
    string ProjectCode,
    DateOnly FromDate,
    DateOnly? ToDate,
    int Percentage
) : IRequest<AllocationDetailResponse>;
```

### 5.2 Handlers Are Sealed

All command/query handlers are `sealed class`. They are registered by MediatR via DI assembly scanning.

### 5.3 Validators Are Paired With Commands

One `FluentValidation.AbstractValidator<TCommand>` per command. Registered automatically via `services.AddValidatorsFromAssembly(typeof(ApplicationAssemblyMarker).Assembly)`.

```csharp
public sealed class CreateAllocationCommandValidator
    : AbstractValidator<CreateAllocationCommand>
{
    public CreateAllocationCommandValidator(ISystemConfigRepository config)
    {
        var minPct = config.GetIntAsync("minAllocationPct").GetAwaiter().GetResult();
        var increment = config.GetIntAsync("allocationIncrement").GetAwaiter().GetResult();

        RuleFor(x => x.Percentage)
            .GreaterThanOrEqualTo(minPct)
            .WithMessage($"Percentage must be at least {minPct}%.")
            .Must(p => p % increment == 0)
            .WithMessage($"Percentage must be a multiple of {increment}%.")
            .LessThanOrEqualTo(100);

        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate)
            .When(x => x.ToDate.HasValue)
            .WithMessage("To Date must be on or after From Date.");
    }
}
```

### 5.4 Never Return `null` From Handlers

Handlers return either a typed DTO or throw `NotFoundException`. Callers never null-check results.

### 5.5 Query Handlers May Use Raw SQL

For complex dashboard queries (join-heavy, aggregates), query handlers may bypass the repository and query `PamsDbContext` directly using `FromSqlRaw` or Dapper. This is intentional for performance; keep CQRS separation.

---

## 6. Repository Pattern

### 6.1 Repositories Return Domain Entities (Not DTOs)

Repositories are domain contracts; they return `Allocation`, `Employee`, etc. DTOs are produced by the Application layer via Mapster.

### 6.2 No SaveChanges in Repositories

Repositories only `Add` / `Update` / mark for deletion. Calling `SaveChangesAsync` is the responsibility of `IUnitOfWork`, invoked by `TransactionBehavior`.

```csharp
// CORRECT
await _allocationRepository.AddAsync(allocation, ct);
await _unitOfWork.SaveChangesAsync(ct);

// WRONG
await _dbContext.SaveChangesAsync(); // called inside repository
```

### 6.3 Soft Delete via EF Global Filter

The global query filter on `Allocation` ensures soft-deleted records never appear in normal queries:

```csharp
// PamsDbContext.OnModelCreating
modelBuilder.Entity<Allocation>()
    .HasQueryFilter(a => a.DeletedAt == null);
```

To query soft-deleted records (HR "Show Removed" toggle), use `IgnoreQueryFilters()`:

```csharp
_dbContext.Allocations.IgnoreQueryFilters()
    .Where(a => a.DeletedAt != null && a.ProjectId == projectId)
    .ToListAsync(ct);
```

---

## 7. Error Handling

### 7.1 Exception Hierarchy

```
Exception
└── DomainException (base for all domain violations)
    ├── CapacityExceededException
    ├── CircularReportingException
    └── UnauthorizedOperationException

ApplicationException (base for application-layer issues)
├── NotFoundException
├── ForbiddenException
└── ConflictException
```

### 7.2 Always Use Typed Exceptions

Never throw `Exception` or `InvalidOperationException` directly in domain or application code. Use the typed hierarchy so the middleware can map to precise HTTP status codes.

### 7.3 ExceptionHandlerMiddleware Pattern

```csharp
app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var (status, code) = exception switch
        {
            NotFoundException e       => (404, "ERR_NOT_FOUND"),
            ForbiddenException e      => (403, e.Code),
            CapacityExceededException => (422, "ERR_CAPACITY_EXCEEDED"),
            ConflictException e       => (409, e.Code),
            ValidationException       => (400, "ERR_VALIDATION"),
            _                         => (500, "ERR_INTERNAL")
        };

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = code,
            Detail = IsDevelopment ? exception!.Message : "An error occurred.",
            Type = $"https://pams.internal/errors/{code}",
            Extensions = { ["traceId"] = Activity.Current?.Id }
        });
    });
});
```

### 7.4 Validation Errors Return 400 With Field Details

FluentValidation failures are captured in `ValidationBehavior` and thrown as `ValidationException`. The middleware extracts individual field errors:

```json
{
  "type": "https://pams.internal/errors/ERR_VALIDATION",
  "title": "Validation Failed",
  "status": 400,
  "errors": {
    "Percentage": [
      "Percentage must be at least 25%.",
      "Percentage must be a multiple of 5%."
    ],
    "ToDate": ["To Date must be on or after From Date."]
  }
}
```

---

## 8. Logging

### 8.1 Use Structured Logging (Never String Interpolation)

```csharp
// CORRECT
_logger.LogInformation(
    "Allocation {AllocationId} created for employee {EmpCode} on project {ProjectCode}",
    allocation.AllocationId, employee.EmpCode, project.ProjectCode);

// WRONG – destroys structured log parsing
_logger.LogInformation($"Allocation {allocation.AllocationId} created");
```

### 8.2 Log Levels

| Level         | When to Use                                                             |
| ------------- | ----------------------------------------------------------------------- |
| `Verbose`     | Fine-grained diagnostics (disabled in all envs by default)              |
| `Debug`       | Detailed flow tracing (development only)                                |
| `Information` | Normal operations: commands executed, HTTP requests                     |
| `Warning`     | Unexpected but recoverable: capacity near limit, optional field missing |
| `Error`       | Operation failed: unhandled exception, DB error                         |
| `Fatal`       | System cannot start                                                     |

### 8.3 Never Log PII in Production

Employee email and full name must not appear in log messages in production. Log `empCode` and `employeeId` (GUIDs) instead.

```csharp
// CORRECT
_logger.LogInformation("Employee {EmpCode} deactivated", employee.EmpCode);

// WRONG – PII in log
_logger.LogInformation("Employee {Email} deactivated", employee.Email);
```

### 8.4 Correlation ID

Inject `X-Correlation-Id` header (generate if not present) in `RequestLoggingMiddleware`. Enrich all Serilog log entries with the correlation ID.

---

## 9. Authentication & Authorization

### 9.1 Authorize All Endpoints by Default

Configure in `Program.cs`:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
```

No endpoints are public. All routes require an authenticated Keycloak OIDC token. Do not add `[AllowAnonymous]` unless explicitly approved — Keycloak handles the unauthenticated redirect flow.

### 9.2 Role Enforcement Is Layered

- **Layer 1 (API):** `[Authorize(Policy = "CanAllocate")]` on controller action.
- **Layer 2 (Application):** Handler checks project ownership for PM scope.
- **Layer 3 (Domain):** Entity methods enforce invariants regardless of caller.

Never rely on Layer 1 alone.

### 9.3 ICurrentUserService

```csharp
public interface ICurrentUserService
{
    Guid EmployeeId { get; }
    string EmpCode { get; }
    EmployeeRole Role { get; }
    bool IsHR => Role == EmployeeRole.HR;
    bool IsProjectManager => Role == EmployeeRole.ProjectManager;
}
```

Implemented by reading `HttpContext.User.Claims` in `CurrentUserService`. Injected into handlers via DI. In tests, substitute with `NSubstitute`.

**Keycloak claim mapping:**

- `EmployeeId` ← `sub` claim (Keycloak user UUID)
- `EmpCode` ← custom Keycloak attribute claim `empCode` (must be configured in the Keycloak realm as a user attribute + token mapper)
- `Role` ← first matching value from `realm_access.roles[]` mapped to `EmployeeRole` enum (`HR`, `ProjectManager`, `Staff`)

> When migrating to Azure AD / Entra ID, only the claim source names change in `CurrentUserService`. The `ICurrentUserService` interface and all command/query handlers remain completely unchanged.

---

## 10. Database & EF Core

### 10.1 Always Use DateOnly for Date-Only Columns

Allocation dates have no time component. Use `DateOnly` in C# mapped to PostgreSQL `date`. Never use `DateTime` for date-only values.

```csharp
public DateOnly FromDate { get; private set; }
public DateOnly? ToDate { get; private set; }
```

EF Core 10 + Npgsql support `DateOnly` natively.

### 10.2 UUID Primary Keys

All entities use `Guid` PKs with `DEFAULT gen_random_uuid()` in PostgreSQL. This avoids insert contention (vs sequential int PKs) and simplifies distributed scenarios.

```csharp
public Guid AllocationId { get; private set; } = Guid.NewGuid();
```

### 10.3 Explicit EF Configurations – No Data Annotations on Entities

Use `IEntityTypeConfiguration<T>` classes, not `[Column]`, `[Required]`, etc. attributes on entities. This keeps domain entities clean of EF concerns.

```csharp
// CORRECT – in AllocationConfiguration.cs
public class AllocationConfiguration : IEntityTypeConfiguration<Allocation>
{
    public void Configure(EntityTypeBuilder<Allocation> builder)
    {
        builder.ToTable("allocations", "pams");
        builder.HasKey(a => a.AllocationId);
        builder.Property(a => a.AllocationId).HasColumnName("allocation_id");
        builder.Property(a => a.Percentage).HasColumnName("percentage").HasColumnType("smallint");
        builder.Property(a => a.FromDate).HasColumnName("from_date").HasColumnType("date");
        builder.Property(a => a.ToDate).HasColumnName("to_date").HasColumnType("date");
        // ...
    }
}
```

### 10.4 Never Use Lazy Loading

Lazy loading is disabled globally. Always use explicit `.Include()` or projection queries. Lazy loading causes N+1 queries that are invisible in code review.

### 10.5 Migration Rules

- One migration per logical change (not one per day or sprint).
- Migration file names are descriptive: `AddAuditLogTable`, `AddAllocationSoftDelete`.
- Never edit a migration that has been applied to any shared environment. Add a new migration to fix it.
- Test all migrations with a fresh database before merging.

### 10.6 Indexes Are Declared in EF Configuration

```csharp
builder.HasIndex(a => new { a.EmployeeId, a.FromDate, a.ToDate })
    .HasDatabaseName("idx_alloc_employee_dates")
    .HasFilter("deleted_at IS NULL");
```

---

## 11. Testing

### 11.1 Test Pyramid

```
         /\
        /  \  E2E / Integration (few)
       /────\
      /      \  Integration (Testcontainers) (moderate)
     /────────\
    /          \  Unit Tests (many)
   /────────────\
```

Target: ≥ 80% line coverage on `PAMS.Domain` and `PAMS.Application`.

### 11.2 Unit Test Structure: AAA

```csharp
[Fact]
public void Validate_WhenCapacityExceeded_ThrowsCapacityExceededException()
{
    // Arrange
    var existing = new List<AllocationOverlap> { new(75) };
    var sut = new AllocationCapacityService();

    // Act
    var act = () => sut.Validate(existing, requested: 50, DateOnly.FromDateTime(DateTime.Today), null);

    // Assert
    act.Should().Throw<CapacityExceededException>()
       .WithMessage("*75%*");
}
```

### 11.3 Mock Only the Layer Boundary

In Application unit tests, substitute `IAllocationRepository`, `IUnitOfWork`, `ICurrentUserService`. Do NOT mock EF Core directly.

```csharp
var allocationRepo = Substitute.For<IAllocationRepository>();
allocationRepo.GetOverlappingTotalPercentageAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly?>(), null, Arg.Any<CancellationToken>())
    .Returns(75);
```

### 11.4 Integration Tests Use Real PostgreSQL

```csharp
[Collection("Integration")]
public class AllocationsEndpointTests(PamsApiFactory factory) : IClassFixture<PamsApiFactory>
{
    [Fact]
    public async Task CreateAllocation_WhenCapacityExceeded_Returns422()
    {
        var client = factory.CreateClient();
        // ... seed data, call endpoint, assert 422
    }
}
```

### 11.5 Test Data Builders

Use the `Bogus` library to generate realistic test data:

```csharp
public static class EmployeeBuilder
{
    public static Employee Valid() => new Faker<Employee>()
        .CustomInstantiator(f => Employee.Create(
            empCode: f.Random.AlphaNumeric(6).ToUpper(),
            firstName: f.Name.FirstName(),
            lastName: f.Name.LastName(),
            email: f.Internet.Email(),
            designation: f.Name.JobTitle(),
            role: EmployeeRole.Staff))
        .Generate();
}
```

---

## 12. API Design

### 12.1 REST Resource Naming

- Plural nouns: `/accounts`, `/employees`, `/allocations`
- Nested routes only one level deep: `/employees/{empCode}/allocations` → **removed** (merged into `GET /employees/{empCode}?includeEnded=true`)
- Avoid verbs in URLs; use HTTP methods:
  - `GET /allocations/capacity-check` ← query resource (acceptable)
  - `PATCH /allocations/{id}` with `{"action": "stop"}` ← partial state change
  - `DELETE /allocations/{id}` ← soft-delete (idiomatic REST)
- Deactivation is a state change via `PUT`:
  - `PUT /accounts/{accountCode}` with `isActive: false` (no separate `/deactivate` endpoint)
  - `PUT /projects/{projectCode}` with `isActive: false`
  - `PUT /employees/{empCode}` with `isActive: false`
- Code-based identifiers: Use business codes (`accountCode`, `projectCode`, `empCode`) in path parameters instead of UUIDs. Only allocations and skills use UUID (no natural business code).

### 12.2 HTTP Status Codes

| Scenario                         | Status                    |
| -------------------------------- | ------------------------- |
| Resource created                 | 201 + `Location` header   |
| Successful read / update         | 200                       |
| Successful delete (soft)         | 200 with updated resource |
| Successful hard delete           | 204 No Content            |
| Not found                        | 404                       |
| Validation failed                | 400                       |
| Business rule violation (domain) | 422                       |
| Duplicate / conflict             | 409                       |
| Auth rejected                    | 401                       |
| Insufficient permissions         | 403                       |
| Conflict (duplicate code)        | 409                       |
| Server error                     | 500                       |

### 12.3 Pagination

All list endpoints use offset pagination with standardized parameters (NFR-18/19):

```
GET /employees?page=1&limit=10
```

**Standard constants** (define in `PAMS.Application/Constants/PaginationDefaults.cs`):

```csharp
public static class PaginationDefaults
{
    public const int DefaultPage = 1;
    public const int DefaultLimit = 10;
    public const int MaxLimit = 100;
}
```

Response includes:

```json
{
  "data": [...],
  "pagination": {
    "page": 1,
    "limit": 10,
    "totalRecords": 142,
    "totalPages": 15
  }
}
```

- Use `limit` (not `pageSize`) for items per page.
- Use `totalRecords` (not `totalCount`) for the total row count.
- Default limit: 10, max limit: 100. Reject `limit > 100` with 400.
- Always return `totalPages = (int)Math.Ceiling((double)totalRecords / limit)`.

### 12.4 Date Format

All dates in request/response bodies: ISO 8601 `YYYY-MM-DD` (no time component for `DateOnly` fields). Timestamps: ISO 8601 with UTC offset `2026-02-25T14:30:00Z`.

### 12.5 Idempotency

`PUT` endpoints are idempotent by design. `POST` allocations are not; clients must check for 409 and handle duplicates.

### 12.6 Single-Endpoint Strategy (NFR-20)

Do **not** create separate `/search` endpoints. Each resource list endpoint (`/employees`, `/accounts`, `/projects`) supports an optional `search` query parameter for text-based searching alongside existing filter parameters.

**Implementation pattern:**

```csharp
// In GetEmployeesQuery.cs
public record GetEmployeesQuery(
    int Page = PaginationDefaults.DefaultPage,
    int Limit = PaginationDefaults.DefaultLimit,
    string? Search = null,       // text search (name, empCode, skill)
    EmployeeRole? Role = null,
    bool? IsActive = null,
    bool? BenchOnly = null,
    DateOnly? WindowFrom = null,
    DateOnly? WindowTo = null
) : IRequest<PagedResult<EmployeeSummary>>;
```

```csharp
// In repository: build a single IQueryable pipeline
var query = _context.Employees.AsQueryable();

if (!string.IsNullOrWhiteSpace(search))
{
    query = query.Where(e =>
        e.FullName.Contains(search) ||
        e.EmpCode.StartsWith(search) ||
        e.EmployeeSkills.Any(es => es.Skill.SkillName == search));
}

if (role.HasValue)
    query = query.Where(e => e.Role == role.Value);

if (isActive.HasValue)
    query = query.Where(e => e.IsActive == isActive.Value);

// Apply pagination last
var totalRecords = await query.CountAsync(ct);
var data = await query
    .Skip((page - 1) * limit)
    .Take(limit)
    .ToListAsync(ct);
```

- Minimum search length: 2 characters — return 400 if `search` is provided but shorter.
- Skill search is exact match on skill name; name search is partial (contains); empCode is prefix.
- When searching by skill, order bench employees (0% allocated) first.

### 12.7 Team Lead Scoped Queries (FR-020)

Project-scoped team lead queries use the `project_team_members` table. When loading "My Team" data for a specific user:

```csharp
// TeamLeadValidator service pattern
public async Task<bool> IsTeamLeadOnProject(
    Guid employeeId, Guid projectId, CancellationToken ct)
{
    return await _context.ProjectTeamMembers
        .AnyAsync(ptm => ptm.TeamLeadId == employeeId
                      && ptm.ProjectId == projectId, ct);
}

public async Task ValidateNoCircularReporting(
    Guid projectId, Guid teamLeadId, Guid reporteeId, CancellationToken ct)
{
    // Check: reporteeId is not already a team lead of teamLeadId on same project
    var circular = await _context.ProjectTeamMembers
        .AnyAsync(ptm => ptm.ProjectId == projectId
                      && ptm.TeamLeadId == reporteeId
                      && ptm.ReporteeId == teamLeadId, ct);

    if (circular)
        throw new CircularReportingException(projectId, teamLeadId, reporteeId);
}
```

- The composite unique constraint `(project_id, team_lead_id, reportee_id)` prevents duplicates at DB level.
- The check constraint `team_lead_id != reportee_id` prevents self-assignment at DB level.
- Always validate circular reporting in the application layer before insert.

---

## 13. Security Practices

### 13.1 Parameterized Queries Only

All database queries use EF Core LINQ or `ExecuteSqlRaw` with parameters. String-concatenated SQL is forbidden.

```csharp
// CORRECT
await _dbContext.Allocations
    .Where(a => a.EmployeeId == employeeId)
    .ToListAsync(ct);

// WRONG
await _dbContext.Database.ExecuteSqlRawAsync(
    $"SELECT * FROM allocations WHERE employee_id = '{employeeId}'");
```

### 13.2 Secrets Management

| Environment | Where Secrets Live                          |
| ----------- | ------------------------------------------- |
| Development | `dotnet user-secrets` (not in source)       |
| CI/CD       | GitHub Actions / Azure DevOps secrets vault |
| Production  | Environment variables or Azure Key Vault    |

Keycloak client secrets must never be committed to source control. `KeycloakSettings:Authority` and any client credentials are injected at runtime via environment variables or Azure Key Vault.

### 13.3 HTTPS Enforcement

```csharp
app.UseHttpsRedirection();
app.UseHsts(); // production only
```

### 13.4 Security Headers Middleware

```csharp
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});
```

---

## 14. Performance Guidelines

### 14.1 Async All the Way

All I/O operations are `async`/`await`. Never use `.Result` or `.Wait()` on `Task` in production code.

### 14.2 AsNoTracking for Read-Only Queries

```csharp
// Queries that only read (no subsequent update/delete on tracked entities)
_dbContext.Employees
    .AsNoTracking()
    .Where(e => e.IsActive)
    .ToListAsync(ct);
```

### 14.3 Select Projections Not Full Entities for Lists

```csharp
// CORRECT – only fetch what you need for the list response
_dbContext.Employees
    .Where(e => e.IsActive)
    .Select(e => new EmployeeSummary(
        e.EmployeeId, e.EmpCode, e.FirstName, e.LastName, e.Designation))
    .ToListAsync(ct);

// WRONG – loads entire Employee with all navigation props
_dbContext.Employees.ToListAsync(ct);
```

### 14.4 Cancel on Client Disconnect

All repository and handler methods accept and pass `CancellationToken`. Bind it to the HTTP request's cancellation token:

```csharp
public async Task<IActionResult> Search([FromQuery] SearchRequest req, CancellationToken ct)
{
    var result = await _mediator.Send(req.Adapt<SearchEmployeesQuery>(), ct);
    return Ok(result);
}
```

---

## 15. Code Review Checklist

Before approving a PR, verify:

- [ ] No business logic in controllers
- [ ] Domain entities have private setters; mutation via methods only
- [ ] No `SaveChangesAsync` inside repositories
- [ ] No lazy loading; all includes are explicit
- [ ] All methods accept and pass `CancellationToken`
- [ ] `AsNoTracking()` used in query-only paths
- [ ] Structured logging (no string interpolation in log messages)
- [ ] No PII in log messages
- [ ] New commands have validators
- [ ] New endpoints have auth attributes
- [ ] PM scope enforced in handler (not just controller)
- [ ] Unit tests added/updated for changed business logic
- [ ] No migration files edited after initial creation
- [ ] No secrets in source code
