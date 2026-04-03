# AK Gaming e.V. Management-Tool

---

## 🧩 Architecture Overview

### Structure

```text
AkGaming.Management/
├─ WebApi/
├─ Frontend/
├─ Frontend.Components/
├─ Modules/
│  └─ MemberManagement/
│     ├─ Api/
│     ├─ Application/
│     ├─ Contracts/
│     ├─ Domain/
│     ├─ Infrastructure/
│     └─ Tests/
└─ build/
```

### Core Principles

* **Clean Architecture:** strict separation of Domain, Application, Infrastructure, and API layers. Please make yourself familiar with the Clean Architecture design before contributing any major changes or new features/modules.
* **Modular Monolith:** each module owns its data, logic, and API endpoints.
* **Dependency Inversion:** Application layer depends on abstractions, not implementations.
* **Result Pattern:** uniform error handling and functional-style result propagation.
* **Minimal APIs:** lightweight modular endpoint definition per module.

---

## 🧠 Key Technologies

* **.NET 10.0** (ASP.NET Core Minimal APIs)
* **Entity Framework Core** (per-module DbContexts)
* **OpenID Connect / OpenIddict** for interactive login and API authorization
* **Swashbuckle / Swagger** for interactive API docs
* **Coolify** for CI/CD deployment management
* **PostgreSQL** for persistent data

---

## 🧱 Modules

Each module is self-contained and follows the same structure:

* **Domain:** Entities, Enums, and ValueObjects.
* **Application:** Use cases, services, validators, and contracts.
* **Infrastructure:** Data access (EF Core repositories, migrations).
* **Api:** Endpoints and dependency injection setup.
* **Contracts:** DTOs and interfaces shared between modules.

### Example: MemberManagement

Responsible for managing member data, membership status, and user linkage.

Endpoints:

```
GET    /api/members                     # All members
GET    /api/members/{id}                # Get by ID
POST   /api/members                     # Create member
PUT    /api/members/{id}                # Update member
DELETE /api/members/{id}                # Delete member
POST   /api/memberships/{id}/status     # Update membership status
```

---

## ⚙️ Deployment Setup

This project uses **Coolify** for deployment.

### Environments

| Environment | Branch    | Deploy Trigger   | Database             | Auth                              |
| ----------- | --------- | ---------------- | -------------------- |-----------------------------------|
| Development | `develop` | Auto via webhook | `AkGaming.Management_Dev` | Special Dev Auth                  |
| Production  | `main`    | Manual trigger   | `AkGaming.Management`     | Production Auth via User Accounts |

Each environment has its own configuration (connection strings, OIDC authority/client credentials, API URLs, etc.) injected through environment variables.

---

## 🔐 Identity Integration

The management tool now authenticates against `AkGaming.Identity` via standard OpenID Connect:

* `Frontend/` is the interactive OIDC client.
* `WebApi/` validates access tokens issued by `AkGaming.Identity`.
* The required API scope is `management_api`.

### Frontend

Important configuration keys:

* `OpenIdConnect:Authority`
* `OpenIdConnect:ClientId`
* `OpenIdConnect:ClientSecret`
* `OpenIdConnect:CallbackPath`
* `OpenIdConnect:SignedOutCallbackPath`
* `Api:BaseUrl`
* `IdentityApi:BaseUrl`

### Web API

Important configuration keys:

* `OpenIddictValidation:Issuer`
* `ConnectionStrings:DefaultConnection`
* `Database:Provider`

### Identity Administration

The management frontend now includes dedicated identity administration pages for:

* users
* roles
* OIDC clients
* OIDC scopes

OIDC client and scope management is backed by the identity server admin API.

### Protected Bootstrap Entries

The management tool must not be able to break its own login path. For that reason:

* the management bootstrap OIDC client is protected in the UI and admin API
* the `management_api` scope is protected in the UI and admin API
* protected entries are restored from the identity server configuration on startup

That means ordinary clients/scopes can be edited in the management UI, but the entries required to keep the management tool working are configuration-owned and recoverable from the identity host.

---

## 🧩 Development Setup

### Prerequisites

* .NET 10 SDK
* Docker (for local database)
* Rider (Recommended) / Visual Studio / VS Code

### Running locally

#### Backend
```bash
dotnet build
dotnet run --project WebApi/AkGaming.Management.WebApi.csproj
```

Open `https://localhost:5001/swagger` for the interactive API UI.

#### Frontend
```bash
dotnet run --project Frontend/AkGaming.Management.Frontend.csproj
```

The frontend expects a matching OIDC client registration in `AkGaming.Identity`.

### Database

The system uses EF Core migrations per module and provider.

```bash
dotnet ef database update \
  --project Modules/MemberManagement/Migrations/Postgres/AkGaming.Management.Modules.MemberManagement.Migrations.Postgres.csproj \
  --context MemberManagementDbContext
```

```bash
dotnet ef database update \
  --project Modules/MemberManagement/Migrations/Sqlite/AkGaming.Management.Modules.MemberManagement.Migrations.Sqlite.csproj \
  --context MemberManagementDbContext
```

```bash
dotnet ef migrations add <MigrationName> \
  --project Modules/MemberManagement/Migrations/Postgres/AkGaming.Management.Modules.MemberManagement.Migrations.Postgres.csproj \
  --context MemberManagementDbContext
```

```bash
dotnet ef migrations add <MigrationName> \
  --project Modules/MemberManagement/Migrations/Sqlite/AkGaming.Management.Modules.MemberManagement.Migrations.Sqlite.csproj \
  --context MemberManagementDbContext
```

Notes:
- Keep the PostgreSQL and SQLite migration projects aligned whenever the model changes.
- The Web API now uses `Migrate()` for SQLite as well as PostgreSQL.
- Legacy local `management.db` files created via `EnsureCreated()` are reset automatically in Development/Testing the first time the new migration pipeline runs.
- The deploy workflow can apply PostgreSQL migrations automatically when `MANAGEMENT_TEST_DB_CONNECTION_STRING` and `MANAGEMENT_PRODUCTION_DB_CONNECTION_STRING` are configured as GitHub secrets.

---

## 🧠 Contribution Guidelines

### Branching Strategy

* `main` → Active development, auto-deployed to staging
* `production` → Production-ready code, live environment
* Feature branches: `feature/<name>` → merged into `main`

### Pull Requests

1. Create a feature branch from `main`.
2. Implement your feature (respecting module boundaries).
3. Add or update tests.
4. Open a PR targeting `main`.
5. Ensure all checks pass (build, tests).

### Coding Standards

* Follow **Clean Architecture** conventions.
* Place all business logic inside the **Application** layer.
* Keep **Domain** entities persistence-agnostic.
* Use **Result<T>** as return type for consistent error handling.
* Document all methods and classes. Try to avoid comments explaining single code blocks.
* Ensure new endpoints are properly documented in Swagger.

### Adding New Modules

1. Scaffold a new folder in `/Modules/<ModuleName>/`.
2. Create subprojects for Domain, Application, Infrastructure, Api, and Contracts.
3. Follow the same DI and endpoint pattern as existing modules.
4. Register the module in the Web API host using:

   ```csharp
   builder.Services.Add<ModuleName>Module(builder.Configuration);
   app.Map<ModuleName>Endpoints();
   ```

---

## 🧩 Testing

* We use nUnit and Moq for unit testing
* Make sure to write unit tests that cover most of the business logic you introduce. DotCover is a great tool to track coverage of your tests
* Run tests with:

```bash
dotnet test
```

---
