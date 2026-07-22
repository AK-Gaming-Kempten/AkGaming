---
name: add-management-module
description: Add or scaffold a module in the AK Gaming Management application, including layered projects, API and frontend integration, EF Core SQLite/PostgreSQL migrations, tests, solution files, and deployment workflow steps. Use for new Management modules, module extraction, or when completing missing module registration, migrations, and CI deployment wiring.
---

# Add a Management Module

Follow the repository `AGENTS.md` first. Inspect the closest existing module before creating files; use `BoardManagement` as the complete layered example and another domain-specific module when its behavior is a closer match.

## 1. Define the module boundary

- Confirm the first feature, permissions, owning data, API surface, and external integrations.
- Keep the module in `AkGaming.Management/Modules/<ModuleName>`.
- Reuse shared Core projects only for genuinely cross-application contracts or infrastructure.
- Use the shared Management database unless the user explicitly requests an independently deployed datastore. Do not invent `<module>_design` as a CI database.

## 2. Create the project structure

Create only layers the module needs, normally:

```text
Api/
Application/
Contracts/
Domain/
Infrastructure/
Migrations/Postgres/
Migrations/Sqlite/
Tests/Application/
Tests/Domain/
Tests/Infrastructure/
Tests/WebApi/
```

Match project references and package versions from the closest module. Keep controllers in `Api`, use application interfaces/services for behavior, keep EF persistence in `Infrastructure`, and expose frontend-safe DTOs from `Contracts`.

## 3. Integrate the host applications

Check and update every applicable integration point:

- Module service-registration extension and `AkGaming.Management/WebApi/Program.cs`.
- Web API project references for the API and both migration assemblies.
- Runtime migration registration/application in Web API startup.
- Authorization policies and Identity permission constants/seed data.
- Frontend contract reference, API client, authorization policy, navigation, routes, and components.
- Transactional outbox and consumer contracts when the module emits external notifications.

Follow established frontend components and interaction patterns instead of introducing module-specific substitutes.

## 4. Add provider-specific migrations

Treat SQLite and PostgreSQL as supported providers. Create separate migration projects and migrations for both.

The PostgreSQL design-time factory must use the workflow-provided connection string:

```csharp
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? "Host=localhost;Database=<module>_design;Username=postgres;Password=postgres";
```

Use the fallback only for local design-time commands. Never hardcode the fallback directly in `UseNpgsql`; otherwise CI silently attempts `localhost:5432` and the `<module>_design` database even when the workflow provides the real connection string.

Keep the SQLite factory provider-specific with a local `Data Source=...` fallback. Do not feed a PostgreSQL environment connection string into the SQLite factory.

Generate equivalent migrations and snapshots in both provider projects. Check for pending model changes before handoff.

## 5. Wire deployment migrations

Update `.github/workflows/deploy-management.yml` for both test and production:

- Add explicit restore and test steps for the new module test project to the workflow test job; Management CI enumerates module test projects and will not discover it automatically.
- Restore the new PostgreSQL migration project.
- Apply it after the shared database tunnel and connection-string validation steps.
- Guard it with the same Management connection-string condition as other modules.
- Pass `ConnectionStrings__DefaultConnection` explicitly from `MANAGEMENT_TEST_DB_CONNECTION_STRING` or `MANAGEMENT_PRODUCTION_DB_CONNECTION_STRING`.
- Use the correct `--project` and `--context` values.

Do not add a separate module database secret or special migration job unless the module actually owns a separate deployed database.

## 6. Update solution and build metadata

- Add every new project to root `AkGaming.slnx`.
- Add every project to `AkGaming.Management/AkGaming.Management.sln` when that solution remains in use.
- Update Docker build inputs or host project references when the build depends on explicitly copied project files.
- Ensure workflow change detection includes the new paths through the existing Management patterns.

## 7. Test provider and integration behavior

- Follow the repository test layout, `[Description]`, Arrange/Act/Assert, and Moq controller-test rules.
- Add SQLite-backed repository tests for `DateTimeOffset`, `decimal`, ordering, comparisons, mappings, and non-trivial queries.
- Test application behavior and controller result mapping separately.
- Verify authorization and host registration when introducing permissions or endpoints.

## 8. Validate before handoff

Run focused tests first, then build both hosts affected by the module. At minimum verify:

```bash
dotnet test AkGaming.Management/Modules/<ModuleName>/Tests/<TestsProject>.csproj
dotnet build AkGaming.Management/WebApi/AkGaming.Management.WebApi.csproj
dotnet build AkGaming.Management/Frontend/AkGaming.Management.Frontend.csproj
```

Also verify:

- Both provider migration projects build.
- EF reports no pending model changes for either provider.
- A PostgreSQL migration command honors `ConnectionStrings__DefaultConnection` rather than connecting to the design fallback.
- `git diff --check` passes.
- No unrelated user changes were overwritten.

Report migrations, required secrets/configuration, deployment order, test results, and known pre-existing warnings. When adding Identity permissions or seeded roles, deploy Identity before depending on those claims in Management.
