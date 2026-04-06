# OpenIddict Server Migration Checklist

This checklist covers the server-side migration of `AkGaming.Identity` from the current custom token/redirect bridge to an OpenIddict-based OAuth 2.0 / OpenID Connect provider.

Scope:
- Server only.
- Preserve existing users.
- Preserve existing Discord links.
- Use interactive Authorization Code flow.
- Support per-client PKCE requirements.
- Migrate the Identity UI away from static token-handling pages.

Constraints:
- Keep the current `User`, `Role`, `UserRole`, `ExternalLogin`, audit log, and email verification tables.
- Add OpenIddict tables to the same database.
- Do not migrate active sessions or existing refresh tokens into OpenIddict.
- Keep legacy `/auth/*` endpoints temporarily during transition.

## Target Architecture

- OpenIddict runs inside the existing `AkGaming.Identity` host.
- Local password/Discord login establishes an Identity-site cookie session.
- OpenIddict serves `/connect/*` endpoints and issues authorization codes, access tokens, ID tokens, and refresh tokens.
- Protected APIs in the Identity host validate tokens via OpenIddict validation instead of the custom JWT bearer setup.
- Static HTML auth pages are replaced with Razor Pages or MVC views.

## Phase 1: Add OpenIddict Infrastructure

- [x] Add `OpenIddict.AspNetCore` to `AkGaming.Identity/Api/AkGaming.Identity.Api.csproj`.
- [x] Add `OpenIddict.Validation.AspNetCore` to `AkGaming.Identity/Api/AkGaming.Identity.Api.csproj`.
- [x] Add `OpenIddict.EntityFrameworkCore` to `AkGaming.Identity/Infrastructure/AkGaming.Identity.Infrastructure.csproj`.
- [x] Update `AkGaming.Identity/Infrastructure/Persistence/AuthDbContext.cs` to register OpenIddict entity sets via `options.UseOpenIddict()`.
- [x] Add an EF Core migration that creates OpenIddict applications, authorizations, scopes, and tokens tables.
- [x] Keep all current identity-domain tables unchanged unless a narrowly scoped migration is required.
- [x] Introduce configuration for OpenIddict signing and encryption credentials.
- [x] Use development certificates only in development; use persisted real credentials in production.

## Phase 2: Replace Host Authentication Model

- [x] Remove the current assumption that the API host is bearer-only in `AkGaming.Identity/Api/Program.cs`.
- [x] Add a local cookie authentication scheme for the Identity site's own login session.
- [x] Register OpenIddict core/server/validation services in `AkGaming.Identity/Api/Program.cs`.
- [x] Replace the custom JWT bearer setup in `AkGaming.Identity/Api/Program.cs` with OpenIddict validation for new tokens.
- [x] Keep the current legacy JWT path only as long as existing clients still need it.
- [x] Add `AddControllersWithViews()` and `AddRazorPages()` to support server-rendered UI and endpoint pass-through.
- [x] Add status code pages integration for OpenIddict errors.

## Phase 3: Introduce OpenIddict Protocol Endpoints

- [x] Add an authorization controller for `/connect/authorize`.
- [x] Add a token endpoint handler for `/connect/token`.
- [x] Add a userinfo endpoint handler for `/connect/userinfo`.
- [x] Add an end-session endpoint handler for `/connect/logout`.
- [x] Enable authorization endpoint pass-through.
- [x] Enable token endpoint pass-through only where custom exchange logic is required.
- [x] Enable logout/end-session pass-through if custom logout UX is required.
- [x] Keep OpenIddict token storage enabled.
- [x] Configure Authorization Code flow.
- [x] Configure Refresh Token flow.
- [x] Support PKCE enforcement via client requirements.
- [x] Register standard scopes: `openid`, `profile`, `email`, `roles`.
- [x] Register custom API scopes separately, e.g. `management_api`.

## Phase 4: Seed OpenIddict Clients and Scopes

- [x] Add a startup seeder for OpenIddict applications in `AkGaming.Identity/Infrastructure`.
- [x] Add a startup seeder for OpenIddict scopes in `AkGaming.Identity/Infrastructure`.
- [ ] Replace `Bridge:AllowedRedirectUris` as the source of truth with real client registrations.
- [ ] For each first-party client, define:
  - [ ] `client_id`
  - [ ] redirect URIs
  - [ ] post-logout redirect URIs
  - [ ] endpoint permissions
  - [ ] grant type permissions
  - [ ] response type permissions
  - [ ] scope permissions
  - [ ] PKCE requirement
- [ ] Use implicit consent only for trusted first-party clients.
- [x] Use explicit consent for third-party clients by default.

## Phase 5: Move UI from Static HTML to Server-Rendered Pages

- [x] Replace `AkGaming.Identity/Api/wwwroot/ui/login.html` with a Razor Page or MVC view.
- [x] Replace `AkGaming.Identity/Api/wwwroot/ui/register.html` with a Razor Page or MVC view.
- [x] Replace `AkGaming.Identity/Api/wwwroot/ui/index.html` with an account/manage page.
- [x] Replace the current callback/token-fragment handling page with normal server-side login and authorization resumption.
- [x] Keep existing CSS/assets from `wwwroot/ui` where useful.
- [x] Remove browser-side token storage from the Identity site's own UI.
- [x] Stop using `localStorage` for access/refresh tokens in Identity-hosted pages.
- [x] Add pages for:
  - [x] login
  - [x] register
  - [x] consent
  - [x] logout
  - [x] account/manage
  - [x] error

## Phase 6: Split Authentication Logic from Token Issuance

- [x] Refactor `AkGaming.Identity/Application/Auth/AuthService.cs` so password login no longer directly issues JWT/refresh tokens for the new path.
- [x] Keep password validation, lockout, email verification, default role assignment, and audit logging logic.
- [x] Introduce a local sign-in service or equivalent application-layer abstraction for issuing the Identity-site cookie.
- [x] Introduce an OpenIddict principal factory that converts `User` plus granted scopes into a `ClaimsPrincipal`.
- [x] Implement explicit claim destinations for access tokens and ID tokens.
- [x] Stop reusing the legacy `JwtTokenService` for the new OIDC issuance path.
- [x] Stop reusing the legacy custom refresh token store for the new OIDC issuance path.

## Phase 7: Rework Authorization Request Handling

- [x] In `/connect/authorize`, if the user is not authenticated locally, challenge to the local login page and preserve the original authorization request.
- [x] After local sign-in, resume the authorization request.
- [x] Load the requesting client from OpenIddict application storage.
- [x] Validate requested scopes and consent requirements.
- [x] Create permanent OpenIddict authorizations for remembered consent.
- [x] Return `Forbid(...)` with OpenIddict error properties for protocol-level errors like `consent_required`.
- [x] Use `prompt=none`, `prompt=login`, and consent behavior correctly.

## Phase 8: Preserve and Rework Discord Flow

- [x] Keep `AkGaming.Identity/Domain/Entities/ExternalLogin.cs` unchanged as the source of linked Discord identities.
- [x] Keep account resolution rules in `AkGaming.Identity/Application/Auth/AuthService.cs`:
  - [x] existing Discord link logs the user into the matching account
  - [x] existing email plus manual-link requirement blocks auto-link
  - [x] optional auto-create creates a local account and then links Discord
- [x] Change Discord callback behavior so it signs the user into the local cookie instead of returning browser tokens for the Identity-hosted flow.
- [x] Keep Discord link flow for already authenticated local users.
- [x] Update `DiscordOAuthState` usage so it stores local return-path / authorization-resume information for the Identity-hosted flow.
- [x] Keep audit logging for Discord login/link success and failure.

## Phase 9: Update Identity APIs to the New Validation Path

- [x] Move new bearer validation for admin/account APIs to OpenIddict validation.
- [x] Ensure `/auth/me` or its replacement account endpoint works with the new tokens while the UI is being migrated.
- [x] Review role claims and authorization policies against the new claim format.
- [x] If needed, add a custom API scope such as `management_api` and require it where appropriate.
- [x] Keep legacy auth endpoints temporarily for backward compatibility, but do not extend them further.

## Phase 10: Deprecate Legacy Browser Token Bridge

- [x] Mark these endpoints as transitional:
  - [x] `/auth/login`
  - [x] `/auth/register`
  - [x] `/auth/refresh`
  - [x] `/auth/logout`
  - [x] `/auth/redirect/finalize`
- [ ] Stop sending tokens in URL fragments.
- [ ] Stop treating `Bridge:AllowedRedirectUris` as the client registry.
- [x] Stop storing refresh tokens in browser-managed storage for Identity-hosted UI.
- [ ] Remove the legacy bridge flow only after all clients have migrated to `/connect/*`.

## Tests

- [x] Update `AkGaming.Identity/Tests/Api.IntegrationTests/TestApiFactory.cs` to seed OpenIddict clients/scopes for tests.
- [x] Add integration tests for unauthenticated `/connect/authorize` redirecting to login.
- [x] Add integration tests for successful authorization code flow with PKCE.
- [x] Add integration tests for token exchange at `/connect/token`.
- [x] Add integration tests for refresh token issuance and redemption.
- [x] Add integration tests for `/connect/userinfo`.
- [x] Add integration tests for consent persistence and repeated authorizations.
- [x] Add integration tests for logout/end-session behavior.
- [x] Add integration tests for Discord login under the new local-cookie flow.
- [x] Add integration tests for Discord linking while already locally authenticated.
- [x] Add unit tests for the new OpenIddict principal factory and claim destination logic.

## Suggested PR Breakdown

### PR 1: OpenIddict Foundation

- [x] Add packages.
- [x] Add OpenIddict DB tables/migration.
- [x] Add application/scope seeding.
- [x] Register OpenIddict core/server/validation in the host.

### PR 2: Local Cookie + UI Migration

- [x] Add local cookie auth.
- [x] Replace static login/register/account pages with Razor Pages or MVC views.
- [x] Add error page integration.

### PR 3: `/connect/*` Endpoints

- [x] Implement authorization, token, userinfo, and logout endpoints.
- [x] Add consent handling and authorization persistence.
- [x] Enforce Authorization Code + PKCE.

### PR 4: Discord Integration Rewrite

- [x] Rework Discord callback to sign in locally for the Identity-hosted flow.
- [x] Rework link flow to operate on local authenticated session.
- [x] Preserve all current account resolution behavior.

### PR 5: Validation + Legacy Deprecation

- [x] Move APIs to OpenIddict validation.
- [x] Keep temporary legacy compatibility.
- [ ] Remove old bridge code only after client migration.

## Remaining Work

- [ ] Register real first-party clients in `OpenIddict:Applications` and use those registrations as the only source of truth for redirect URIs and logout URIs.
- [ ] Remove the deprecated legacy `/auth/*` token bridge endpoints after client migration.
- [ ] Remove fragment-based token delivery entirely by deleting the legacy browser bridge once clients have moved to `/connect/*`.

## Files Expected to Change

- `AkGaming.Identity/Api/AkGaming.Identity.Api.csproj`
- `AkGaming.Identity/Api/Program.cs`
- `AkGaming.Identity/Api/Controllers/AuthorizationController.cs`
- `AkGaming.Identity/Api/Controllers/ErrorController.cs`
- `AkGaming.Identity/Api/Pages/Account/Login.cshtml`
- `AkGaming.Identity/Api/Pages/Account/Register.cshtml`
- `AkGaming.Identity/Api/Pages/Account/Manage.cshtml`
- `AkGaming.Identity/Api/Pages/Consent/Index.cshtml`
- `AkGaming.Identity/Infrastructure/AkGaming.Identity.Infrastructure.csproj`
- `AkGaming.Identity/Infrastructure/DependencyInjection.cs`
- `AkGaming.Identity/Infrastructure/Persistence/AuthDbContext.cs`
- `AkGaming.Identity/Migrations/*`
- `AkGaming.Identity/Infrastructure/OpenIddict/*`
- `AkGaming.Identity/Application/Auth/AuthService.cs`
- `AkGaming.Identity/Application/Abstractions/*`
- `AkGaming.Identity/Application/ExternalAuth/DiscordOAuthState.cs`
- `AkGaming.Identity/Tests/Api.IntegrationTests/TestApiFactory.cs`
- `AkGaming.Identity/Tests/Api.IntegrationTests/*`
- `AkGaming.Identity/Tests/Application.UnitTests/*`

## Decisions Already Settled

- Use OpenIddict.
- Use Authorization Code flow for interactive clients.
- Support per-client PKCE requirements.
- Keep existing users.
- Keep existing Discord links.
- Handle clients later; this checklist is server-first.
- Declutter the UI by moving away from static token-handling HTML pages.

## Reference Documentation

- https://documentation.openiddict.com/guides/choosing-the-right-flow
- https://documentation.openiddict.com/integrations/aspnet-core
- https://documentation.openiddict.com/guides/getting-started/creating-your-own-server-instance
- https://documentation.openiddict.com/configuration/proof-key-for-code-exchange.html
- https://documentation.openiddict.com/configuration/application-permissions.html
- https://documentation.openiddict.com/configuration/authorization-storage.html
- https://documentation.openiddict.com/configuration/claim-destinations.html
- https://documentation.openiddict.com/configuration/token-storage.html
