# PAMS – Scaffold Plan

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

## 2. Prerequisites

| Tool              | Version | Install Command / Source                         |
| ----------------- | ------- | ------------------------------------------------ |
| .NET SDK          | 10.0.x  | https://dot.net/download (RTM build)             |
| PostgreSQL        | 17.x    | https://postgresql.org or Docker `postgres:17`   |
| Docker Desktop    | latest  | Required for Testcontainers in integration tests |
| EF Core CLI tools | 10.0.x  | `dotnet tool install --global dotnet-ef`         |
| Git               | ≥ 2.40  | https://git-scm.com                              |

---

## 3. Solution Structure

```
project-management/
├── src/
│   ├── PAMS.Domain/
│   ├── PAMS.Application/
│   ├── PAMS.Infrastructure/
│   └── PAMS.API/
├── tests/
│   ├── PAMS.UnitTests/
│   └── PAMS.IntegrationTests/
├── project-notes/
│   ├── specs.md
│   ├── architecture.md
│   ├── api-spec.yaml
│   ├── scaffold-plan.md
│   ├── best-practices.md
│   └── er-diagram.md
├── docker-compose.yml
├── docker-compose.override.yml
├── .env.example
├── .gitignore
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
└── PAMS.slnx
```

---

## 4. Folder Structure Detail

### 4.1 PAMS.Domain

```
PAMS.Domain/
├── Common/
│   ├── IAuditableEntity.cs
│   ├── ISoftDeletable.cs
│   └── IUnitOfWork.cs
├── Entities/
│   ├── Account.cs
│   ├── Allocation.cs
│   ├── AuditLog.cs
│   ├── Employee.cs
│   ├── EmployeeSkill.cs
│   ├── Project.cs
│   ├── ProjectTeamMember.cs
│   ├── Skill.cs
│   └── SystemConfig.cs
├── Enums/
│   ├── AccountType.cs
│   ├── AllocationStatus.cs      (computed enum: Bench, Partial, Full)
│   ├── EmployeeRole.cs
│   └── ProjectStatus.cs
├── Exceptions/
│   ├── CapacityExceededException.cs
│   ├── CircularReportingException.cs
│   ├── DomainException.cs
│   └── UnauthorizedOperationException.cs
├── Repositories/
│   ├── IAccountRepository.cs
│   ├── IAllocationRepository.cs
│   ├── IEmployeeRepository.cs
│   ├── IProjectRepository.cs
│   ├── IProjectTeamMemberRepository.cs
│   ├── ISkillRepository.cs
│   └── ISystemConfigRepository.cs
├── Services/
│   ├── AllocationCapacityService.cs
│   ├── AllocationStopService.cs
│   ├── ReportingChainValidator.cs
│   └── TeamLeadValidator.cs
└── PAMS.Domain.csproj
```

### 4.2 PAMS.Application

```
PAMS.Application/
├── Behaviors/
│   ├── LoggingBehavior.cs
│   ├── TransactionBehavior.cs
│   └── ValidationBehavior.cs
├── Commands/
│   ├── Accounts/
│   │   ├── CreateAccount/
│   │   │   ├── CreateAccountCommand.cs
│   │   │   ├── CreateAccountCommandHandler.cs
│   │   │   └── CreateAccountCommandValidator.cs
│   │   └── UpdateAccount/              (isActive=false to deactivate)
│   │       ├── UpdateAccountCommand.cs
│   │       ├── UpdateAccountCommandHandler.cs
│   │       └── UpdateAccountCommandValidator.cs
│   ├── Allocations/
│   │   ├── CreateAllocation/
│   │   │   ├── CreateAllocationCommand.cs
│   │   │   ├── CreateAllocationCommandHandler.cs
│   │   │   └── CreateAllocationCommandValidator.cs
│   │   ├── UpdateAllocation/
│   │   │   ├── UpdateAllocationCommand.cs
│   │   │   ├── UpdateAllocationCommandHandler.cs
│   │   │   └── UpdateAllocationCommandValidator.cs
│   │   ├── StopAllocation/               (PATCH /allocations/{id})
│   │   │   ├── StopAllocationCommand.cs
│   │   │   └── StopAllocationCommandHandler.cs
│   │   └── RemoveAllocation/             (DELETE /allocations/{id})
│   │       ├── RemoveAllocationCommand.cs
│   │       └── RemoveAllocationCommandHandler.cs
│   ├── Employees/
│   │   ├── CreateEmployee/
│   │   └── UpdateEmployee/              (isActive=false to deactivate)
│   ├── Projects/
│   │   ├── CreateProject/
│   │   └── UpdateProject/              (status=Inactive to deactivate)
│   ├── ProjectTeamMembers/
│   │   ├── AddProjectTeamMember/
│   │   │   ├── AddProjectTeamMemberCommand.cs
│   │   │   ├── AddProjectTeamMemberCommandHandler.cs
│   │   │   └── AddProjectTeamMemberCommandValidator.cs
│   │   └── RemoveProjectTeamMember/
│   │       ├── RemoveProjectTeamMemberCommand.cs
│   │       └── RemoveProjectTeamMemberCommandHandler.cs
│   ├── EmployeeSkills/               (MVP2 – FR-023)
│   │   └── ManageOwnSkills/
│   │       ├── ManageOwnSkillsCommand.cs
│   │       ├── ManageOwnSkillsCommandHandler.cs
│   │       └── ManageOwnSkillsCommandValidator.cs
│   ├── Skills/
│   │   ├── CreateSkill/
│   │   └── UpdateSkill/
│   └── SystemConfig/
│       └── UpdateSystemConfig/
├── DTOs/
│   ├── Accounts/
│   │   ├── AccountResponse.cs
│   │   └── AccountSummary.cs
│   ├── Allocations/
│   │   ├── AllocationDetailResponse.cs
│   │   ├── AllocationSummary.cs
│   │   └── CapacityCheckResponse.cs
│   ├── Employees/
│   │   ├── EmployeeDetailResponse.cs
│   │   ├── EmployeeSummary.cs
│   │   └── PMAllocationDashboardResponse.cs
│   ├── Projects/
│   │   ├── ProjectDetailResponse.cs
│   │   └── ProjectSummary.cs
│   ├── Dashboard/
│   │   ├── ProjectViewResponse.cs
│   │   └── EmployeeViewResponse.cs
│   ├── ProjectTeamMembers/
│   │   └── ProjectTeamMemberResponse.cs
│   └── Skills/
│       └── SkillResponse.cs
├── Exceptions/
│   ├── ConflictException.cs
│   ├── ForbiddenException.cs
│   └── NotFoundException.cs
├── Interfaces/
│   ├── IAuditLogService.cs
│   ├── ICurrentUserService.cs
│   └── IDateTimeProvider.cs
├── Mappings/
│   └── MappingConfig.cs
├── Queries/
│   ├── Accounts/
│   │   ├── GetAccounts/
│   │   │   ├── GetAccountsQuery.cs
│   │   │   └── GetAccountsQueryHandler.cs
│   │   └── GetAccountByCode/
│   ├── Allocations/
│   │   ├── GetAllocationsByEmployee/
│   │   ├── GetAllocationsByProject/
│   │   └── GetCapacityCheck/
│   │       ├── GetCapacityCheckQuery.cs
│   │       └── GetCapacityCheckQueryHandler.cs
│   ├── Dashboard/
│   │   ├── GetEmployeeViewDashboard/
│   │   │   ├── GetEmployeeViewDashboardQuery.cs
│   │   │   └── GetEmployeeViewDashboardQueryHandler.cs
│   │   └── GetProjectViewDashboard/
│   │       ├── GetProjectViewDashboardQuery.cs
│   │       └── GetProjectViewDashboardQueryHandler.cs
│   ├── Employees/
│   │   ├── GetEmployees/           (unified list + search + filter, NFR-20)
│   │   ├── GetEmployeeByCode/
│   │   └── GetEmployeeAllocationStatus/
│   ├── Projects/
│   │   ├── GetProjects/
│   │   └── GetProjectByCode/
│   ├── ProjectTeamMembers/
│   │   └── GetProjectTeamMembers/
│   │       ├── GetProjectTeamMembersQuery.cs
│   │       └── GetProjectTeamMembersQueryHandler.cs
│   └── Skills/
│       └── GetSkills/
└── PAMS.Application.csproj
```

### 4.3 PAMS.Infrastructure

```
PAMS.Infrastructure/
├── Extensions/
│   └── InfrastructureServiceExtensions.cs
├── Persistence/
│   ├── Configurations/
│   │   ├── AccountConfiguration.cs
│   │   ├── AllocationConfiguration.cs
│   │   ├── AuditLogConfiguration.cs
│   │   ├── EmployeeConfiguration.cs
│   │   ├── EmployeeSkillConfiguration.cs
│   │   ├── ProjectConfiguration.cs
│   │   ├── ProjectTeamMemberConfiguration.cs
│   │   ├── SkillConfiguration.cs
│   │   └── SystemConfigConfiguration.cs
│   ├── Migrations/
│   │   └── (EF Core generated – do not edit manually)
│   ├── Repositories/
│   │   ├── AccountRepository.cs
│   │   ├── AllocationRepository.cs
│   │   ├── EmployeeRepository.cs
│   │   ├── ProjectRepository.cs
│   │   ├── ProjectTeamMemberRepository.cs
│   │   ├── SkillRepository.cs
│   │   └── SystemConfigRepository.cs
│   ├── Seed/
│   │   ├── SkillSeeder.cs
│   │   └── SystemConfigSeeder.cs
│   ├── PamsDbContext.cs
│   └── UnitOfWork.cs
├── Services/
│   ├── AuditLogService.cs
│   ├── CurrentUserService.cs
│   └── DateTimeProvider.cs
└── PAMS.Infrastructure.csproj
```

### 4.4 PAMS.API

```
PAMS.API/
├── Controllers/
│   ├── AccountsController.cs
│   ├── AllocationsController.cs
│   ├── DashboardController.cs
│   ├── EmployeesController.cs
│   ├── ProjectsController.cs
│   ├── ProjectTeamMembersController.cs
│   ├── SkillsController.cs
│   └── SystemConfigController.cs
├── Extensions/
│   ├── ApplicationBuilderExtensions.cs
│   └── ServiceCollectionExtensions.cs
├── Middleware/
│   ├── ExceptionHandlerMiddleware.cs
│   └── RequestLoggingMiddleware.cs
├── Models/
│   └── ErrorCodes.cs
├── appsettings.json
├── appsettings.Development.json
├── appsettings.Production.json
├── Program.cs
└── PAMS.API.csproj
```

### 4.5 PAMS.UnitTests

```
PAMS.UnitTests/
├── Domain/
│   ├── AllocationCapacityServiceTests.cs
│   ├── AllocationStopServiceTests.cs
│   ├── ReportingChainValidatorTests.cs
│   └── TeamLeadValidatorTests.cs
├── Application/
│   ├── Commands/
│   │   ├── CreateAllocationCommandHandlerTests.cs
│   │   ├── StopAllocationCommandHandlerTests.cs
│   │   └── RemoveAllocationCommandHandlerTests.cs
│   └── Validators/
│       ├── CreateAllocationCommandValidatorTests.cs
│       ├── CreateEmployeeCommandValidatorTests.cs
│       └── AddProjectTeamMemberCommandValidatorTests.cs
├── Helpers/
│   └── AllocationBuilder.cs    (test data builder / fixture)
└── PAMS.UnitTests.csproj
```

### 4.6 PAMS.IntegrationTests

```
PAMS.IntegrationTests/
├── Infrastructure/
│   ├── PamsApiFactory.cs         (WebApplicationFactory<Program>)
│   └── DatabaseFixture.cs        (Testcontainers PostgreSQL)
├── Endpoints/
│   ├── AccountsEndpointTests.cs
│   ├── AllocationsEndpointTests.cs
│   ├── EmployeesEndpointTests.cs
│   ├── ProjectsEndpointTests.cs
│   └── ProjectTeamMembersEndpointTests.cs
├── Helpers/
│   ├── AuthHelper.cs             (generate test JWTs)
│   └── SeedData.cs
└── PAMS.IntegrationTests.csproj
```

---

## 5. Project Files

### 5.1 global.json

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestMinor"
  }
}
```

### 5.2 Directory.Build.props

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsAsErrors />
    <AnalysisLevel>latest</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
</Project>
```

### 5.3 Directory.Packages.props (Central Package Management)

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <!-- EF Core + PostgreSQL -->
    <PackageVersion Include="Microsoft.EntityFrameworkCore"                        Version="10.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design"                 Version="10.0.0" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL"               Version="10.0.0" />

    <!-- CQRS -->
    <PackageVersion Include="MediatR"                                              Version="12.4.1" />
    <PackageVersion Include="MediatR.Extensions.Microsoft.DependencyInjection"    Version="12.4.1" />

    <!-- Validation -->
    <PackageVersion Include="FluentValidation"                                     Version="11.11.0" />
    <PackageVersion Include="FluentValidation.DependencyInjectionExtensions"      Version="11.11.0" />

    <!-- Auth: Keycloak OAuth2/OIDC — PAMS is a resource server; token validation via JWKS auto-discovery -->
    <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer"       Version="10.0.0" />

    <!-- Mapping -->
    <PackageVersion Include="Mapster"                                              Version="7.4.0" />
    <PackageVersion Include="Mapster.DependencyInjection"                         Version="1.0.2" />

    <!-- Logging -->
    <PackageVersion Include="Serilog.AspNetCore"                                   Version="8.0.3" />
    <PackageVersion Include="Serilog.Sinks.Console"                               Version="6.0.0" />
    <PackageVersion Include="Serilog.Sinks.File"                                  Version="6.0.0" />
    <PackageVersion Include="Serilog.Enrichers.Environment"                       Version="3.0.0" />
    <PackageVersion Include="Serilog.Enrichers.Thread"                            Version="4.0.0" />

    <!-- API docs -->
    <PackageVersion Include="Scalar.AspNetCore"                                    Version="2.1.4" />
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi"                        Version="10.0.0" />

    <!-- Health Checks -->
    <PackageVersion Include="AspNetCore.HealthChecks.NpgSql"                      Version="8.0.2" />

    <!-- Testing -->
    <PackageVersion Include="xunit"                                                Version="2.9.2" />
    <PackageVersion Include="xunit.runner.visualstudio"                           Version="2.8.2" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk"                              Version="17.12.0" />
    <PackageVersion Include="FluentAssertions"                                     Version="7.0.0" />
    <PackageVersion Include="NSubstitute"                                          Version="5.3.0" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing"                    Version="10.0.0" />
    <PackageVersion Include="Testcontainers.PostgreSql"                           Version="3.10.0" />
    <PackageVersion Include="Bogus"                                                Version="35.6.1" />
  </ItemGroup>
</Project>
```

### 5.4 PAMS.Domain.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>PAMS.Domain</AssemblyName>
    <RootNamespace>PAMS.Domain</RootNamespace>
  </PropertyGroup>
  <!-- No NuGet dependencies: pure domain -->
</Project>
```

### 5.5 PAMS.Application.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>PAMS.Application</AssemblyName>
    <RootNamespace>PAMS.Application</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\PAMS.Domain\PAMS.Domain.csproj" />
    <PackageReference Include="MediatR" />
    <PackageReference Include="FluentValidation" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" />
    <PackageReference Include="Mapster" />
    <PackageReference Include="Mapster.DependencyInjection" />
  </ItemGroup>
</Project>
```

### 5.6 PAMS.Infrastructure.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>PAMS.Infrastructure</AssemblyName>
    <RootNamespace>PAMS.Infrastructure</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\PAMS.Domain\PAMS.Domain.csproj" />
    <ProjectReference Include="..\PAMS.Application\PAMS.Application.csproj" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
    <PackageReference Include="Serilog.AspNetCore" />
  </ItemGroup>
</Project>
```

### 5.7 PAMS.API.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <AssemblyName>PAMS.API</AssemblyName>
    <RootNamespace>PAMS.API</RootNamespace>
    <UserSecretsId>pams-api-secrets</UserSecretsId>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\PAMS.Application\PAMS.Application.csproj" />
    <ProjectReference Include="..\PAMS.Infrastructure\PAMS.Infrastructure.csproj" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
    <PackageReference Include="Scalar.AspNetCore" />
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
    <PackageReference Include="Serilog.AspNetCore" />
    <PackageReference Include="Serilog.Sinks.Console" />
    <PackageReference Include="Serilog.Sinks.File" />
    <PackageReference Include="Serilog.Enrichers.Environment" />
    <PackageReference Include="Serilog.Enrichers.Thread" />
    <PackageReference Include="AspNetCore.HealthChecks.NpgSql" />
  </ItemGroup>
</Project>
```

---

## 6. CLI Scaffold Commands

Run these in order from the solution root (`project-management/`).

> **Platform note:** Line continuations use `\` (bash/zsh/PowerShell 7+). On Windows PowerShell 5.x, replace `\` with the backtick `` ` `` character, or run each project on a single line.

---

### Step 0: Initialize workspace files

```bash
# Pin SDK version — prevents implicit SDK roll-forward across machines
# Source: `dotnet new globaljson` template (verified MS Learn)
dotnet new globaljson --sdk-version 10.0.100

# Standard .NET .gitignore (bin/, obj/, user secrets, etc.)
dotnet new gitignore

# Directory.Build.props — applies shared MSBuild properties to all projects
# Source: `dotnet new buildprops` template (verified MS Learn)
dotnet new buildprops
```

---

### Step 1: Create solution and projects

```bash
# In .NET 10, `dotnet new sln` defaults to the .slnx format (XML-based, no GUIDs,
# supported by MSBuild 17.12+ and dotnet CLI SDK 9.0.200+).
# Produces PAMS.slnx. To force legacy format: dotnet new sln -n PAMS --format sln
dotnet new sln -n PAMS

# ── Source projects ─────────────────────────────────────────────────────────────
# --framework net10.0 is explicit (it is the default for .NET 10 SDK; safe to keep)
dotnet new classlib -n PAMS.Domain         -o src/PAMS.Domain         --framework net10.0
dotnet new classlib -n PAMS.Application    -o src/PAMS.Application    --framework net10.0
dotnet new classlib -n PAMS.Infrastructure -o src/PAMS.Infrastructure --framework net10.0

# webapi default since .NET 8+ is minimal APIs.
# --use-controllers enables the controller-based approach (required for PAMS).
# --framework net10.0 is explicit (default for .NET 10 SDK).
# Source: -controllers|--use-controllers option verified in MS Learn dotnet-new-sdk-templates.
dotnet new webapi -n PAMS.API -o src/PAMS.API --framework net10.0 --use-controllers

# ── Test projects ────────────────────────────────────────────────────────────────
# xunit default framework for .NET 10 SDK is net10.0; --framework is explicit but safe.
dotnet new xunit -n PAMS.UnitTests        -o tests/PAMS.UnitTests        --framework net10.0
dotnet new xunit -n PAMS.IntegrationTests -o tests/PAMS.IntegrationTests --framework net10.0

# ── Register projects in solution ────────────────────────────────────────────────
# Multi-project add in a single call (supported since dotnet CLI SDK 9.0.200+).
# -s|--solution-folder organizes projects into named solution folders in the IDE.
# Source: dotnet sln add options verified in MS Learn dotnet-sln reference.
dotnet sln PAMS.slnx add -s src \
  src/PAMS.Domain/PAMS.Domain.csproj \
  src/PAMS.Application/PAMS.Application.csproj \
  src/PAMS.Infrastructure/PAMS.Infrastructure.csproj \
  src/PAMS.API/PAMS.API.csproj

dotnet sln PAMS.slnx add -s tests \
  tests/PAMS.UnitTests/PAMS.UnitTests.csproj \
  tests/PAMS.IntegrationTests/PAMS.IntegrationTests.csproj
```

---

### Step 2: Add project references

```bash
# Clean Architecture dependency rule:
#   Domain  ←  Application  ←  Infrastructure  ←  API
#
# `dotnet add <PROJECT> reference <REF>` is the established syntax.
# .NET 10 also introduces the new alias `dotnet reference add`; both are valid.

# Application depends on Domain
dotnet add src/PAMS.Application/PAMS.Application.csproj \
  reference src/PAMS.Domain/PAMS.Domain.csproj

# Infrastructure depends on Domain + Application
dotnet add src/PAMS.Infrastructure/PAMS.Infrastructure.csproj \
  reference src/PAMS.Domain/PAMS.Domain.csproj
dotnet add src/PAMS.Infrastructure/PAMS.Infrastructure.csproj \
  reference src/PAMS.Application/PAMS.Application.csproj

# API depends on Application + Infrastructure
dotnet add src/PAMS.API/PAMS.API.csproj \
  reference src/PAMS.Application/PAMS.Application.csproj
dotnet add src/PAMS.API/PAMS.API.csproj \
  reference src/PAMS.Infrastructure/PAMS.Infrastructure.csproj

# Unit tests depend on Domain + Application (no infrastructure, no HTTP)
dotnet add tests/PAMS.UnitTests/PAMS.UnitTests.csproj \
  reference src/PAMS.Domain/PAMS.Domain.csproj
dotnet add tests/PAMS.UnitTests/PAMS.UnitTests.csproj \
  reference src/PAMS.Application/PAMS.Application.csproj

# Integration tests depend on API + Infrastructure (full stack via Testcontainers)
dotnet add tests/PAMS.IntegrationTests/PAMS.IntegrationTests.csproj \
  reference src/PAMS.API/PAMS.API.csproj
dotnet add tests/PAMS.IntegrationTests/PAMS.IntegrationTests.csproj \
  reference src/PAMS.Infrastructure/PAMS.Infrastructure.csproj
```

---

### Step 3: Install EF Core tools

```bash
# Global install — suitable for local developer workstations
dotnet tool install --global dotnet-ef

# Verify installed version matches TargetFramework
dotnet ef --version
```

### Step 4: Create initial EF migration

```bash
dotnet ef migrations add InitialCreate \
  --project src/PAMS.Infrastructure \
  --startup-project src/PAMS.API \
  --output-dir Persistence/Migrations

dotnet ef database update \
  --project src/PAMS.Infrastructure \
  --startup-project src/PAMS.API
```

### Step 5: Run the application

```bash
dotnet run --project src/PAMS.API
# API available at: https://localhost:5001
# Scalar UI at:     https://localhost:5001/scalar/v1
# Health:           https://localhost:5001/health
```

### Step 6: Run tests

```bash
# All tests
dotnet test

# Unit tests only
dotnet test tests/PAMS.UnitTests

# Integration tests only (requires Docker)
dotnet test tests/PAMS.IntegrationTests

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

---

## 7. appsettings.json Template

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=pams;Username=pams_user;Password=CHANGE_ME;Search Path=pams"
  },
  "KeycloakSettings": {
    "Authority": "https://keycloak.internal/realms/pams",
    "Audience": "pams-api",
    "RequireHttpsMetadata": true
  },
  "AllowedHosts": "*",
  "HealthChecks": {
    "Enabled": true
  }
}
```

> **Security:** `KeycloakSettings:Authority` and any Keycloak client credentials must be managed via `dotnet user-secrets` in development and environment variables / Azure Key Vault in production. Never commit Keycloak secrets to source control.

---

## 8. .env.example (Docker Compose)

```env
POSTGRES_DB=pams
POSTGRES_USER=pams_user
POSTGRES_PASSWORD=change_me_in_production
POSTGRES_PORT=5432
KEYCLOAK_ADMIN=admin
KEYCLOAK_ADMIN_PASSWORD=change_me_in_production
KEYCLOAK_PORT=8080
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=https://+:5001;http://+:5000
```

---

## 9. docker-compose.yml

```yaml
version: "3.9"
services:
  postgres:
    image: postgres:17
    environment:
      POSTGRES_DB: ${POSTGRES_DB:-pams}
      POSTGRES_USER: ${POSTGRES_USER:-pams_user}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    ports:
      - "${POSTGRES_PORT:-5432}:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER:-pams_user}"]
      interval: 10s
      timeout: 5s
      retries: 5

  keycloak:
    image: quay.io/keycloak/keycloak:26.1
    command: start-dev --import-realm
    environment:
      KEYCLOAK_ADMIN: ${KEYCLOAK_ADMIN:-admin}
      KEYCLOAK_ADMIN_PASSWORD: ${KEYCLOAK_ADMIN_PASSWORD}
    ports:
      - "${KEYCLOAK_PORT:-8080}:8080"
    volumes:
      - ./keycloak/realm-pams.json:/opt/keycloak/data/import/realm-pams.json:ro
      - keycloak_data:/opt/keycloak/data
    healthcheck:
      test:
        ["CMD-SHELL", "curl -sf http://localhost:8080/health/ready || exit 1"]
      interval: 30s
      timeout: 10s
      retries: 5
    # realm-pams.json must define: realm=pams, client=pams-api, roles: HR / ProjectManager / Staff

  api:
    build:
      context: .
      dockerfile: src/PAMS.API/Dockerfile
    depends_on:
      postgres:
        condition: service_healthy
      keycloak:
        condition: service_healthy
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__DefaultConnection: "Host=postgres;Port=5432;Database=${POSTGRES_DB:-pams};Username=${POSTGRES_USER:-pams_user};Password=${POSTGRES_PASSWORD};Search Path=pams"
      KeycloakSettings__Authority: "http://keycloak:8080/realms/pams"
      KeycloakSettings__Audience: "pams-api"
      KeycloakSettings__RequireHttpsMetadata: "false"
    ports:
      - "5001:5001"
      - "5000:5000"

volumes:
  postgres_data:
  keycloak_data:
```

---

## 10. .gitignore Additions

```gitignore
# EF Migrations bundle
*.bundle

# User secrets
%APPDATA%/Microsoft/UserSecrets/

# Test results
TestResults/
coverage/
*.coverage
```

---

## 11. Test Configuration

### xunit.runner.json (place in each test project root)

```json
{
  "parallelizeAssembly": false,
  "parallelizeTestCollections": true,
  "maxParallelThreads": 4
}
```

### Integration Test: PamsApiFactory.cs skeleton

```csharp
public class PamsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .WithDatabase("pams_test")
        .WithUsername("pams_test")
        .WithPassword("pams_test")
        .Build();

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace EF registration with test container connection string
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<PamsDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<PamsDbContext>(options =>
                options.UseNpgsql(_dbContainer.GetConnectionString()));
        });
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }
}
```

---

## 12. Naming Conventions Summary

| Artifact             | Convention               | Example                                 |
| -------------------- | ------------------------ | --------------------------------------- |
| Solution             | PascalCase               | `PAMS.slnx`                             |
| Project              | `PAMS.<Layer>`           | `PAMS.Domain`                           |
| Namespace            | `PAMS.<Layer>.<Area>`    | `PAMS.Application.Commands.Allocations` |
| Entity class         | PascalCase singular      | `Allocation`                            |
| Repository interface | `I<Entity>Repository`    | `IAllocationRepository`                 |
| Command              | `<Verb><Noun>Command`    | `CreateAllocationCommand`               |
| Query                | `Get<Noun>Query`         | `GetEmployeesQuery`                     |
| Handler              | `<Command/Query>Handler` | `CreateAllocationCommandHandler`        |
| Validator            | `<Command>Validator`     | `CreateAllocationCommandValidator`      |
| DTO (response)       | `<Noun>Response`         | `AllocationDetailResponse`              |
| DTO (list item)      | `<Noun>Summary`          | `EmployeeSummary`                       |
| Controller           | `<Resource>Controller`   | `AllocationsController`                 |
| DB table             | snake_case plural        | `allocations`, `employee_skills`        |
| DB column            | snake_case               | `employee_id`, `from_date`              |
| DB index             | `idx_<table>_<cols>`     | `idx_alloc_employee_dates`              |
| Migration            | PascalCase descriptor    | `InitialCreate`, `AddAuditLogTable`     |
