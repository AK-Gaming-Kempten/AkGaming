# AK Gaming e.V. Management-Tool

---

## 🧩 Architecture Overview

### Structure

```text
AkGaming.Management/
├─ AkGaming.Management.WebApi/
├─ AkGaming.Management.Frontend/
├─ AkGaming.Management.Frontend.Components/
├─ AkGaming.Management.Modules/
│  ├─ AkGaming.Core.Common/
│  └─ AkGaming.Management.Modules.MemberManagement/
│     ├─ Api/
│     ├─ Application/
│     ├─ Contracts/
│     ├─ Domain/
│     ├─ Infrastructure/
│     └─ Tests/
└─ AkGaming.Management.Common/
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

Each environment has its own configuration (connection string, JWT settings, etc.) injected through environment variables.

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
dotnet run --project AkGaming.Management.WebApi/AkGaming.Management.WebApi.csproj
```

Open `https://localhost:5001/swagger` for the interactive API UI.

#### Frontend
[Not yet implemented]

### Database

The system uses EF Core migrations per module:

```bash
dotnet ef migrations add Init --project AkGaming.Management.Modules/AkGaming.Management.Modules.MemberManagement/Infrastructure/AkGaming.Management.Modules.MemberManagement.Infrastructure.csproj
```

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

1. Scaffold a new folder in `/AkGaming.Management.Modules/<ModuleName>/`.
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
