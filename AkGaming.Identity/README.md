# AkGaming.Identity

Identity provider service for AK Gaming e.V. applications.

It supports:
- Email/password authentication with hashed passwords.
- JWT access and refresh tokens.
- Refresh token rotation and reuse detection.
- Discord OAuth2 login and account linking.
- Email verification via token and direct verification link.
- Role-based authorization with admin role management endpoints.
- Audit logging for security-relevant operations.
- SQLite for development and PostgreSQL for production.

## Architecture

Projects:
- `AkGaming.Identity.Api`: Minimal API host, static UI pages, endpoint mappings.
- `AkGaming.Identity.Application`: Use-case logic and auth orchestration.
- `AkGaming.Identity.Domain`: Entities and domain constants.
- `AkGaming.Identity.Infrastructure`: EF Core persistence, security, Discord, SMTP.
- `AkGaming.Identity.Contracts`: Shared request/response DTOs (NuGet-packable).
- `AkGaming.Identity.Application.UnitTests`: Unit tests.
- `AkGaming.Identity.Api.IntegrationTests`: Integration tests.

## Tech Stack

- .NET 10
- ASP.NET Core Minimal APIs
- EF Core
- SQLite / PostgreSQL
- JWT Bearer auth
- Discord OAuth2
- SMTP for verification emails

## Local Development

Prerequisites:
- .NET SDK 10.x
- Optional: PostgreSQL (if not using SQLite)

Run:

```bash
dotnet restore
dotnet build AkGaming.Identity.sln
dotnet run --project Api/AkGaming.Identity.Api.csproj
```

Dev URLs:
- App root: `https://localhost:5001/`
- Login page: `https://localhost:5001/login`
- Register page: `https://localhost:5001/register`
- Swagger (Development only): `https://localhost:5001/swagger`

## Configuration

Configuration is loaded from `appsettings*.json` and environment variables.

Main sections:
- `Database`
- `ConnectionStrings`
- `Jwt`
- `Discord`
- `AuthHardening`
- `Smtp`
- `App`
- `Bridge`

### Important Environment Variables

Use double underscores for nested config keys:

- `Database__Provider=Sqlite|Postgres`
- `ConnectionStrings__IdentityDb=...`
- `Jwt__Issuer=AkGaming.Identity`
- `Jwt__Audience=AkGaming.Clients`
- `Jwt__SecretKey=<min-32-chars>`
- `Jwt__AccessTokenMinutes=15`
- `Jwt__RefreshTokenDays=7`
- `Discord__ClientId=...`
- `Discord__ClientSecret=...`
- `Discord__RedirectUri=https://identity.akgaming.de/auth/discord/callback`
- `Discord__AutoCreateUser=true`
- `Discord__RequireManualLinkForExistingEmail=true`
- `AuthHardening__MaxFailedLoginAttempts=5`
- `AuthHardening__LockoutMinutes=15`
- `AuthHardening__RequireVerifiedEmailForLogin=true`
- `AuthHardening__EmailVerificationTokenHours=24`
- `AuthHardening__ExposeEmailVerificationToken=false`
- `Smtp__Enabled=true`
- `Smtp__Host=smtp.example.com`
- `Smtp__Port=587`
- `Smtp__UseSsl=true`
- `Smtp__Username=...`
- `Smtp__Password=...`
- `Smtp__FromEmail=no-reply@akgaming.de`
- `Smtp__FromName=AK Gaming Identity`
- `App__PublicBaseUrl=https://identity.akgaming.de`
- `Bridge__AllowedRedirectUris__0=https://management.akgaming.de/authentication/callback`
- `Bridge__AllowedRedirectUris__1=https://*.akgaming.de/authentication/callback`

## Database and Migrations

Apply PostgreSQL migrations:

```bash
dotnet ef database update \
  --project Migrations/Postgres/AkGaming.Identity.Migrations.Postgres.csproj \
  --context AuthDbContext
```

Apply SQLite migrations:

```bash
dotnet ef database update \
  --project Migrations/Sqlite/AkGaming.Identity.Migrations.Sqlite.csproj \
  --context AuthDbContext
```

Create a new PostgreSQL migration:

```bash
dotnet ef migrations add <MigrationName> \
  --project Migrations/Postgres/AkGaming.Identity.Migrations.Postgres.csproj \
  --context AuthDbContext
```

Create a matching SQLite migration:

```bash
dotnet ef migrations add <MigrationName> \
  --project Migrations/Sqlite/AkGaming.Identity.Migrations.Sqlite.csproj \
  --context AuthDbContext
```

Notes:
- Keep PostgreSQL and SQLite migrations in sync whenever the model changes.
- The API applies the configured provider's migrations automatically on startup.
- The deploy workflow can also apply PostgreSQL migrations when `IDENTITY_TEST_DB_CONNECTION_STRING` and `IDENTITY_PRODUCTION_DB_CONNECTION_STRING` are configured as GitHub secrets.

## Docker and Deployment

The API listens on port `8080` in container:
- `ASPNETCORE_URLS=http://+:8080`
- `EXPOSE 8080`

If you run behind a reverse proxy (for example Coolify), route external HTTPS traffic to container port `8080`.

## Authentication Flows

### Email/Password

1. `POST /auth/register`
2. `POST /auth/login`
3. Use `accessToken` as bearer token.
4. Refresh with `POST /auth/refresh`.
5. Revoke refresh token with `POST /auth/logout`.

### Discord OAuth2

1. Start: `GET /auth/discord/start`
2. Callback: `GET /auth/discord/callback?code=...&state=...`
3. Link for authenticated user: `POST /auth/discord/link`

### Email Verification

- Request by email: `POST /auth/email/send-verification`
- Request for current user: `POST /auth/email/send-verification/me`
- Verify by token: `POST /auth/email/verify`
- Verify by link: `GET /auth/email/verify-link?token=...`

## Redirect Bridge Flow (for other apps)

Use this when another app redirects to Identity and wants tokens back after login.

1. Redirect user to:
- `GET /login?redirect_uri=<callback>&state=<opaque-state>`

2. Identity UI logs in user and then calls:
- `POST /auth/redirect/finalize`

3. Identity validates callback URL against `Bridge:AllowedRedirectUris`.

4. Identity returns `redirectUrl` containing tokens in URL fragment:
- `https://your-app/callback#access_token=...&refresh_token=...&expires_at=...&state=...`

Security notes:
- Redirect URI must be absolute `http/https`.
- Wildcards support subdomains only, for example `https://*.akgaming.de/authentication/callback`.
- `https://*.akgaming.de` does not match root domain `https://akgaming.de`.

## API Endpoints

### Auth Endpoints

- `POST /auth/register` body `RegisterRequest` -> `AuthResponse`
- `POST /auth/login` body `LoginRequest` -> `AuthResponse`
- `POST /auth/refresh` body `RefreshRequest` -> `AuthResponse`
- `POST /auth/logout` body `LogoutRequest` -> `204`
- `GET /auth/logout?returnUrl=&refreshToken=` -> redirect
- `POST /auth/redirect/finalize` body `RedirectFinalizeRequest` -> `{ redirectUrl }`
- `GET /auth/me` bearer token -> `CurrentUserResponse`
- `POST /auth/email/send-verification` body `EmailVerificationRequest` -> `EmailVerificationResponse`
- `POST /auth/email/send-verification/me` bearer token -> `EmailVerificationResponse`
- `POST /auth/email/verify` body `VerifyEmailRequest` -> `204`
- `GET /auth/email/verify-link?token=` -> redirect to UI
- `GET /auth/discord/start` -> redirect to Discord
- `GET /auth/discord/callback?code=&state=` -> redirect to `/ui/callback.html#...`
- `POST /auth/discord/link` bearer token -> `DiscordStartResponse`

### Admin Endpoints

Requires `Admin` role.

- `GET /admin/users?page=&pageSize=&search=` -> `AdminUsersResponse`
- `GET /admin/users/{userId}` -> `AdminUserDetailsResponse`
- `GET /admin/users/{userId}/roles` -> `UserRolesResponse`
- `PUT /admin/users/{userId}/roles` body `AdminSetUserRolesRequest` -> `UserRolesResponse`
- `GET /admin/roles` -> `RoleResponse[]`
- `POST /admin/roles` body `AdminCreateRoleRequest` -> `RoleResponse`
- `PUT /admin/roles/{roleId}` body `AdminRenameRoleRequest` -> `RoleResponse`
- `DELETE /admin/roles/{roleId}` -> `204`

## Contracts Package

Contracts live in:
- `AkGaming.Identity.Contracts`

Pack locally:

```bash
dotnet pack Contracts/AkGaming.Identity.Contracts.csproj -c Release
```

Publish workflow:
- File: `.github/workflows/publish-contracts.yml`
- Trigger by tag: `contracts-v*` (example `contracts-v1.0.1`)
- Requires GitHub secret: `NUGET_API_KEY`

## Using Identity From Another Service

Recommended minimum:

1. Redirect user to Identity login URL with your callback URL.
2. Parse fragment on callback page for `access_token`, `refresh_token`, `expires_at`.
3. Store access token short-lived and refresh token securely.
4. Use bearer token on admin endpoints for role/user management.

## Hardening Implemented

- Refresh token rotation.
- Refresh token reuse detection.
- Login lockout policy.
- Rate limiting for auth/admin groups.
- Email verification with expiry.
- Audit log persistence.

## Troubleshooting

- `PendingModelChangesWarning`: create and apply migration for current model.
- `SQLite Error ... no such column`: database schema is older than model; run migrations.
- Docker build `project.assets.json not found`: ensure all referenced `.csproj` files are copied before `dotnet restore`.
- SMTP `User not authenticated`: verify SMTP username/password, port/SSL mode, and sender permissions.
- Redirect denied: check `Bridge__AllowedRedirectUris__*` entries and exact path matching.

## License

MIT
