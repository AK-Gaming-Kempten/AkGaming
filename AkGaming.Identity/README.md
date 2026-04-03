# AkGaming.Identity

OIDC and OAuth 2.0 provider for AK Gaming applications.

The current server is based on OpenIddict and issues authorization codes, access tokens, refresh tokens, and identity tokens. It also keeps the existing AK Gaming user store, role model, audit logging, email verification, and Discord account links.

## Current Role In The System

`AkGaming.Identity` is the central authentication server.

It currently serves three jobs:

- local account and session management for the identity site
- OpenID Connect / OAuth 2.0 token issuance for first-party and third-party clients
- token validation for APIs hosted inside the identity service itself

The management tool is wired to it as:

- `AkGaming.Management.Frontend`: confidential OIDC client using authorization code + PKCE
- `AkGaming.Management.WebApi`: resource server validating access tokens from this issuer

## Projects

- `AkGaming.Identity.Api`: ASP.NET Core host, OIDC endpoints, Razor Pages, admin endpoints
- `AkGaming.Identity.Application`: auth use cases and orchestration
- `AkGaming.Identity.Domain`: entities and domain constants
- `AkGaming.Identity.Infrastructure`: EF Core, OpenIddict persistence, Discord, SMTP, security
- `AkGaming.Identity.Contracts`: shared contracts
- `AkGaming.Identity.Tests`: unit and integration tests

## Protocol Surface

The server is configured in [Program.cs](/home/hexasiel/Programming/AkGaming/AkGaming.Identity/Api/Program.cs).

OpenIddict endpoints:

- `/connect/authorize`
- `/connect/token`
- `/connect/userinfo`
- `/connect/logout`

Enabled flows:

- authorization code
- refresh token

Security settings:

- PKCE required
- access token encryption disabled
- development signing/encryption certs in Development and Testing
- configured PFX certificates outside Development and Testing

## How Management Is Wired To Identity

The integration has three configuration sides.

### 1. Identity registers the management frontend as a client

Client registrations live under `OpenIddict:Applications` in:

- [appsettings.json](/home/hexasiel/Programming/AkGaming/AkGaming.Identity/Api/appsettings.json)
- [appsettings.Development.json](/home/hexasiel/Programming/AkGaming/AkGaming.Identity/Api/appsettings.Development.json)
- [appsettings.Production.json](/home/hexasiel/Programming/AkGaming/AkGaming.Identity/Api/appsettings.Production.json)

The management frontend is currently registered as:

- `ClientId`: `akgaming-management-frontend`
- `ClientType`: `confidential`
- `RequirePkce`: `true`
- scopes: `openid`, `profile`, `email`, `roles`, `offline_access`, `management_api`

That configuration is seeded on startup by [OpenIddictSeeder.cs](/home/hexasiel/Programming/AkGaming/AkGaming.Identity/Infrastructure/OpenIddict/OpenIddictSeeder.cs).

### 2. Management frontend uses matching OIDC client settings

The frontend reads `OpenIdConnect` from:

- [AkGaming.Management/Frontend/appsettings.json](/home/hexasiel/Programming/AkGaming/AkGaming.Management/Frontend/appsettings.json)
- [AkGaming.Management/Frontend/appsettings.Development.json](/home/hexasiel/Programming/AkGaming/AkGaming.Management/Frontend/appsettings.Development.json)

Important values:

- `Authority`
- `ClientId`
- `ClientSecret`
- `CallbackPath`
- `SignedOutCallbackPath`
- `Scopes`

The frontend config is consumed in [ServiceCollectionExtensions.cs](/home/hexasiel/Programming/AkGaming/AkGaming.Management/Frontend/Startup/ServiceCollectionExtensions.cs).

### 3. Management Web API trusts the issuer and requires scope

The Web API reads `OpenIddictValidation:Issuer` from:

- [AkGaming.Management/WebApi/appsettings.json](/home/hexasiel/Programming/AkGaming/AkGaming.Management/WebApi/appsettings.json)
- [AkGaming.Management/WebApi/appsettings.Development.json](/home/hexasiel/Programming/AkGaming/AkGaming.Management/WebApi/appsettings.Development.json)

The Web API validates tokens against the issuer and requires the `management_api` scope in [ServiceCollectionExtensions.cs](/home/hexasiel/Programming/AkGaming/AkGaming.Management/WebApi/Startup/ServiceCollectionExtensions.cs).

### Scope mapping

`management_api` is defined as an OpenIddict scope in [appsettings.json](/home/hexasiel/Programming/AkGaming/AkGaming.Identity/Api/appsettings.json) and mapped into token resources in [OidcPrincipalFactory.cs](/home/hexasiel/Programming/AkGaming/AkGaming.Identity/Api/OpenIddict/OidcPrincipalFactory.cs).

If a client does not request `management_api`, the management Web API should reject the token.

## Local Development

Prerequisites:

- .NET SDK 10.x
- optional PostgreSQL if you do not want SQLite

Run:

```bash
dotnet restore
dotnet build AkGaming.Identity.sln
dotnet run --project Api/AkGaming.Identity.Api.csproj
```

Configuration notes:

- base config in [appsettings.json](/home/hexasiel/Programming/AkGaming/AkGaming.Identity/Api/appsettings.json) uses `https://localhost:5001`
- current development override in [appsettings.Development.json](/home/hexasiel/Programming/AkGaming/AkGaming.Identity/Api/appsettings.Development.json) uses `https://localhost:7288`

What actually matters is that:

- `App:PublicBaseUrl`
- `OpenIddict:Issuer`
- client redirect URIs

all match the URL you run publicly behind.

## Configuration Model

The main config sections are:

- `Database`
- `ConnectionStrings`
- `Jwt`
- `Discord`
- `AuthHardening`
- `Smtp`
- `App`
- `OpenIddict`
- `Bridge`

`Jwt` is still used for token lifetime settings in the current host, even though new interactive clients should use OIDC rather than the old custom token bridge.

### Important Environment Variables

Use double underscores for nesting.

General host:

- `Database__Provider=Sqlite|Postgres`
- `ConnectionStrings__IdentityDb=...`
- `App__PublicBaseUrl=https://identity.akgaming.de`

Token lifetime settings:

- `Jwt__AccessTokenMinutes=15`
- `Jwt__RefreshTokenDays=7`

Discord:

- `Discord__ClientId=...`
- `Discord__ClientSecret=...`

Deployment notes:

- The deploy workflow can apply PostgreSQL migrations automatically when `IDENTITY_TEST_DB_CONNECTION_STRING` and `IDENTITY_PRODUCTION_DB_CONNECTION_STRING` are configured as GitHub secrets.
- If the database is only reachable through SSH, also configure `DB_SSH_HOST`, `DB_SSH_PORT`, `DB_SSH_USER`, `DB_SSH_PRIVATE_KEY`, `DB_SSH_KNOWN_HOSTS`.
- Tunnel target secrets are optional: `DB_TUNNEL_TARGET_HOST` and `DB_TUNNEL_TARGET_PORT`. They default to `127.0.0.1:5432` on the SSH server.
- With the tunnel enabled, the CI connection strings must point to the runner-local forwarded ports: test uses `Host=127.0.0.1;Port=55432;...`, production uses `Host=127.0.0.1;Port=55433;...`.
- `Discord__RedirectUri=https://identity.akgaming.de/auth/discord/callback`
- `Discord__AutoCreateUser=true`
- `Discord__RequireManualLinkForExistingEmail=true`

Login hardening:

- `AuthHardening__MaxFailedLoginAttempts=5`
- `AuthHardening__LockoutMinutes=15`
- `AuthHardening__RequireVerifiedEmailForLogin=true`
- `AuthHardening__EmailVerificationTokenHours=24`
- `AuthHardening__ExposeEmailVerificationToken=false`

SMTP:

- `Smtp__Enabled=true`
- `Smtp__Host=smtp.example.com`
- `Smtp__Port=587`
- `Smtp__UseSsl=true`
- `Smtp__Username=...`
- `Smtp__Password=...`
- `Smtp__FromEmail=no-reply@akgaming.de`
- `Smtp__FromName=AK Gaming Identity`

OpenIddict issuer and certificates:

- `OpenIddict__Issuer=https://identity.akgaming.de`
- `OpenIddict__Credentials__Signing__Path=/app/certificates/openiddict-signing.pfx`
- `OpenIddict__Credentials__Signing__Password=...`
- `OpenIddict__Credentials__Encryption__Path=/app/certificates/openiddict-encryption.pfx`
- `OpenIddict__Credentials__Encryption__Password=...`

Custom scope definitions:

- `OpenIddict__Scopes__0__Name=management_api`
- `OpenIddict__Scopes__0__DisplayName=Management API`
- `OpenIddict__Scopes__0__Resources__0=management_api`

Client definitions:

- `OpenIddict__Applications__0__ClientId=...`
- `OpenIddict__Applications__0__ClientSecret=...`
- `OpenIddict__Applications__0__DisplayName=...`
- `OpenIddict__Applications__0__ConsentType=implicit|explicit|external|systematic`
- `OpenIddict__Applications__0__ClientType=public|confidential`
- `OpenIddict__Applications__0__RequirePkce=true`
- `OpenIddict__Applications__0__RedirectUris__0=https://app.example.com/signin-oidc`
- `OpenIddict__Applications__0__PostLogoutRedirectUris__0=https://app.example.com/signout-callback-oidc`
- `OpenIddict__Applications__0__Scopes__0=openid`
- `OpenIddict__Applications__0__Scopes__1=profile`

Legacy bridge allowlist:

- `Bridge__AllowedRedirectUris__0=https://legacy.example.com/authentication/callback`

## Adding A New OIDC Client

Add a new entry under `OpenIddict:Applications` in the identity config.

The option structure is defined in [OpenIddictSeedOptions.cs](/home/hexasiel/Programming/AkGaming/AkGaming.Identity/Infrastructure/OpenIddict/OpenIddictSeedOptions.cs).

Example:

```json
{
  "ClientId": "my-new-client",
  "ClientSecret": "set-this-for-confidential-clients",
  "DisplayName": "My New Client",
  "ConsentType": "explicit",
  "ClientType": "confidential",
  "RequirePkce": true,
  "AllowAuthorizationCodeFlow": true,
  "AllowRefreshTokenFlow": true,
  "RedirectUris": [
    "https://app.example.com/signin-oidc"
  ],
  "PostLogoutRedirectUris": [
    "https://app.example.com/signout-callback-oidc"
  ],
  "Scopes": [
    "openid",
    "profile",
    "email",
    "roles",
    "offline_access"
  ]
}
```

Guidance:

- use `confidential` for server-side apps that can keep a secret
- use `public` for SPA, mobile, and desktop apps
- keep `RequirePkce=true` for both
- use `openid` if the client needs login
- add `offline_access` if the client needs refresh tokens
- add custom API scopes only if the client should call those APIs

Then configure the client app with matching values for:

- authority
- client id
- client secret if confidential
- callback paths
- requested scopes

### Important Seeder Limitation

The current seeder only creates missing applications and scopes. It does not update existing ones.

This means:

- adding a new client works
- adding a new scope works
- changing an existing client's secret, redirect URIs, consent type, or scopes will not be applied automatically

If you need to change an existing client, either:

- update the OpenIddict application record manually in the database, or
- delete the existing application row and restart the identity service so it is seeded again

This behavior comes from [OpenIddictSeeder.cs](/home/hexasiel/Programming/AkGaming/AkGaming.Identity/Infrastructure/OpenIddict/OpenIddictSeeder.cs).

## Adding A New Protected API

If you want a new client to call another API, do not reuse `management_api` unless it is truly the same API boundary.

Instead:

1. add a new scope under `OpenIddict:Scopes`
2. allow that scope on the relevant clients
3. map the scope to a resource in [OidcPrincipalFactory.cs](/home/hexasiel/Programming/AkGaming/AkGaming.Identity/Api/OpenIddict/OidcPrincipalFactory.cs)
4. require that scope in the target API's authorization policy

Example scope:

```json
{
  "Name": "billing_api",
  "DisplayName": "Billing API",
  "Resources": [
    "billing_api"
  ]
}
```

You must also extend the principal factory similarly to the existing `management_api` mapping.

## Certificates

In Development and Testing, the host uses OpenIddict development certificates automatically.

Outside those environments, it loads configured PFX files from:

- `OpenIddict:Credentials:Signing`
- `OpenIddict:Credentials:Encryption`

Those options are defined in [OpenIddictCredentialOptions.cs](/home/hexasiel/Programming/AkGaming/AkGaming.Identity/Infrastructure/OpenIddict/OpenIddictCredentialOptions.cs).

Production should not store certificate passwords in committed `appsettings` files. Use environment variables or your secret store.

## Database And Startup Behavior

On startup the host:

1. applies EF Core migrations
2. seeds OpenIddict scopes
3. seeds OpenIddict applications

This happens in [Program.cs](/home/hexasiel/Programming/AkGaming/AkGaming.Identity/Api/Program.cs).

## Legacy Endpoints

Some legacy custom auth and bridge endpoints still exist for compatibility, but new clients should use the OpenIddict endpoints instead of the old fragment-based token bridge.

If you integrate a new application, use:

- `/connect/authorize`
- `/connect/token`
- `/connect/userinfo`
- `/connect/logout`

Do not build new integrations on:

- `/auth/redirect/finalize`
- the old fragment token callback flow

## Production Notes

Recommended production overrides:

- move all secrets to environment variables or secret store
- set `OpenIddict__Issuer` and `App__PublicBaseUrl` to the public HTTPS URL
- configure signing and encryption certificate paths/passwords
- configure real database connection strings
- configure Discord and SMTP secrets

At minimum, do not commit real values for:

- `OpenIddict__Credentials__Signing__Password`
- `OpenIddict__Credentials__Encryption__Password`
- `OpenIddict__Applications__*__ClientSecret`
- `Discord__ClientSecret`
- `Smtp__Password`
- `ConnectionStrings__IdentityDb`

## Related Files

- [Program.cs](/home/hexasiel/Programming/AkGaming/AkGaming.Identity/Api/Program.cs)
- [appsettings.json](/home/hexasiel/Programming/AkGaming/AkGaming.Identity/Api/appsettings.json)
- [appsettings.Development.json](/home/hexasiel/Programming/AkGaming/AkGaming.Identity/Api/appsettings.Development.json)
- [appsettings.Production.json](/home/hexasiel/Programming/AkGaming/AkGaming.Identity/Api/appsettings.Production.json)
- [OpenIddictSeedOptions.cs](/home/hexasiel/Programming/AkGaming/AkGaming.Identity/Infrastructure/OpenIddict/OpenIddictSeedOptions.cs)
- [OpenIddictSeeder.cs](/home/hexasiel/Programming/AkGaming/AkGaming.Identity/Infrastructure/OpenIddict/OpenIddictSeeder.cs)
- [OpenIddictCredentialOptions.cs](/home/hexasiel/Programming/AkGaming/AkGaming.Identity/Infrastructure/OpenIddict/OpenIddictCredentialOptions.cs)
- [OidcPrincipalFactory.cs](/home/hexasiel/Programming/AkGaming/AkGaming.Identity/Api/OpenIddict/OidcPrincipalFactory.cs)
