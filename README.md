# PAMS — Project Allocation Management System

A backend API for managing employee-to-project allocations, built with **.NET 10**, **PostgreSQL 17**, and **Keycloak 26** for authentication. Follows Clean Architecture with CQRS (MediatR).

## Tech Stack

| Layer            | Technology                                           |
| ---------------- | ---------------------------------------------------- |
| Runtime          | .NET 10 / ASP.NET Core                               |
| Database         | PostgreSQL 17 with EF Core 10 (Npgsql)               |
| Auth             | Keycloak 26.1 (OIDC / JWT Bearer)                    |
| CQRS             | MediatR 12.4                                         |
| Validation       | FluentValidation 11.11                               |
| Logging          | Serilog                                              |
| Docs             | Swagger UI (Swashbuckle)                             |
| Testing          | xUnit, FluentAssertions, NSubstitute, Testcontainers |
| Containerisation | Docker / Docker Compose                              |

## Project Structure

```
PAMS.slnx
├── src/
│   ├── PAMS.Domain/            # Entities, enums, repository interfaces, domain services
│   ├── PAMS.Application/       # Commands, handlers, DTOs, validators, interfaces
│   ├── PAMS.Infrastructure/    # EF Core DbContext, repositories, migrations, seeders
│   └── PAMS.API/               # Controllers, middleware, auth config, Swagger
├── tests/
│   ├── PAMS.UnitTests/         # 186 unit tests (handlers, validators, domain services)
│   └── PAMS.IntegrationTests/  # 59 integration tests (Testcontainers + WebApplicationFactory)
├── keycloak/
│   └── realm-pams.json         # Pre-configured realm with roles and test users
├── project-notes/              # Specs, architecture, API contract, test reports
├── docker-compose.yml
├── Dockerfile
└── .env
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Run with Docker Compose (recommended)

```bash
# Clone the repo
git clone https://github.com/ffaraazz/pams.git
cd pams

# Start all services (PostgreSQL, Keycloak, API)
docker compose up -d --build

# Wait ~30s for Keycloak to become healthy, then open:
#   Swagger UI  → http://localhost:5000/swagger
#   Keycloak    → http://localhost:8080
```

### Run Locally (without Docker for the API)

```bash
# Start only PostgreSQL and Keycloak
docker compose up -d postgres keycloak

# Run the API locally
dotnet run --project src/PAMS.API
# → http://localhost:5195/swagger (default Kestrel port)
```

### Environment Variables

Configured via `.env` (Docker) or `appsettings.Development.json` (local):

| Variable            | Default             | Description              |
| ------------------- | ------------------- | ------------------------ |
| `POSTGRES_PORT`     | `5433`              | Host port for PostgreSQL |
| `POSTGRES_PASSWORD` | `pams_dev_password` | Database password        |
| `KEYCLOAK_PORT`     | `8080`              | Host port for Keycloak   |
| `API_PORT`          | `5000`              | Host port for the API    |

## Authentication

Keycloak is pre-configured with the `pams` realm, three roles, and three test users:

| Username    | Password   | Role           |
| ----------- | ---------- | -------------- |
| `hr.admin`  | `password` | HR             |
| `pm.alice`  | `password` | ProjectManager |
| `staff.bob` | `password` | Staff          |

**Swagger UI** has built-in OAuth2 ROPC login — click **Authorize**, enter a username/password, and set client ID to `pams-web`.

### Authorization Policies

| Policy           | Roles Allowed          |
| ---------------- | ---------------------- |
| HROnly           | HR                     |
| CanAllocate      | HR, ProjectManager     |
| CanViewDashboard | HR, ProjectManager     |
| Authenticated    | Any authenticated user |

## API Endpoints (29 total)

All endpoints are prefixed with `/api/v1`.

| Method | Path                                         | Policy           |
| ------ | -------------------------------------------- | ---------------- |
| GET    | `/accounts`                                  | CanAllocate      |
| GET    | `/accounts/{accountCode}`                    | CanAllocate      |
| POST   | `/accounts`                                  | HROnly           |
| PUT    | `/accounts/{accountCode}`                    | HROnly           |
| GET    | `/projects`                                  | Authenticated    |
| GET    | `/projects/{projectCode}`                    | Authenticated    |
| POST   | `/projects`                                  | HROnly           |
| PUT    | `/projects/{projectCode}`                    | CanAllocate      |
| GET    | `/employees`                                 | CanAllocate      |
| GET    | `/employees/{empCode}`                       | Authenticated    |
| GET    | `/employees/me`                              | Authenticated    |
| POST   | `/employees`                                 | HROnly           |
| PUT    | `/employees/{empCode}`                       | HROnly           |
| POST   | `/allocations`                               | CanAllocate      |
| GET    | `/allocations/{id}`                          | Authenticated    |
| PUT    | `/allocations/{id}`                          | CanAllocate      |
| PATCH  | `/allocations/{id}`                          | CanAllocate      |
| DELETE | `/allocations/{id}`                          | CanAllocate      |
| GET    | `/allocations/capacity-check`                | CanAllocate      |
| GET    | `/skills`                                    | Authenticated    |
| POST   | `/skills`                                    | HROnly           |
| PUT    | `/skills/{id}`                               | HROnly           |
| GET    | `/system-config`                             | HROnly           |
| PUT    | `/system-config`                             | HROnly           |
| GET    | `/projects/{code}/team-members`              | CanAllocate      |
| POST   | `/projects/{code}/team-members`              | CanAllocate      |
| DELETE | `/projects/{code}/team-members/{lead}/{rep}` | CanAllocate      |
| GET    | `/dashboard/project-view`                    | CanViewDashboard |
| GET    | `/dashboard/employee-view`                   | CanViewDashboard |

## Testing

```bash
# Run all tests (186 unit + 59 integration = 245 total)
dotnet test

# Unit tests only (~300ms)
dotnet test tests/PAMS.UnitTests

# Integration tests only (~22s, requires Docker for Testcontainers)
dotnet test tests/PAMS.IntegrationTests
```

Integration tests use [Testcontainers](https://dotnet.testcontainers.org/) to spin up a disposable PostgreSQL instance — no external database needed.

## Architecture

```
┌─────────────┐
│   PAMS.API  │  Controllers, Middleware, Auth
└──────┬──────┘
       │ depends on
┌──────▼──────────────┐
│  PAMS.Application   │  Commands, Handlers, DTOs, Validators
└──────┬──────────────┘
       │ depends on
┌──────▼──────────────┐
│  PAMS.Domain        │  Entities, Enums, Repository Interfaces, Domain Services
└─────────────────────┘
       ▲ implemented by
┌──────┴──────────────┐
│  PAMS.Infrastructure│  EF Core, Repositories, Migrations, Seeders
└─────────────────────┘
```

- **Domain** — zero external dependencies; pure C# entities and business rules
- **Application** — orchestrates use cases via MediatR handlers; depends only on Domain
- **Infrastructure** — EF Core implementations, Keycloak integration; depends on Domain + Application
- **API** — thin HTTP layer; depends on all layers for DI wiring

## License

This project is for internal/educational use.
