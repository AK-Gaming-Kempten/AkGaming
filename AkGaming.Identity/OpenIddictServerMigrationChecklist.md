# OpenIddict Server Migration Checklist

This checklist covers the server-side migration of `AkGaming.Identity` from the current custom token/redirect bridge to an OpenIddict-based OAuth 2.0 / OpenID Connect provider.

Scope:
- Server only.
- Preserve existing users.
- Preserve existing Discord links.
- Use interactive Authorization Code flow.
- Require PKCE.
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

- [ ] Add `OpenIddict.AspNetCore` to `AkGaming.Identity/Api/AkGaming.Identity.Api.csproj`.
- [ ] Add `OpenIddict.Validation.AspNetCore` to `AkGaming.Identity/Api/AkGaming.Identity.Api.csproj`.
- [ ] Add `OpenIddict.EntityFrameworkCore` to `AkGaming.Identity/Infrastructure/AkGaming.Identity.Infrastructure.csproj`.
- [ ] Update `AkGaming.Identity/Infrastructure/Persistence/AuthDbContext.cs` to register OpenIddict entity sets via `options.UseOpenIddict()`.
- [ ] Add an EF Core migration that creates OpenIddict applications, authorizations, scopes, and tokens tables.
- [ ] Keep all current identity-domain tables unchanged unless a narrowly scoped migration is required.
- [ ] Introduce configuration for OpenIddict signing and encryption credentials.
- [ ] Use development certificates only in development; use persisted real credentials in production.

## Phase 2: Replace Host Authentication Model

- [ ] Remove the current assumption that the API host is bearer-only in `AkGaming.Identity/Api/Program.cs`.
- [ ] Add a local cookie authentication scheme for the Identity site's own login session.
- [ ] Register OpenIddict core/server/validation services in `AkGaming.Identity/Api/Program.cs`.
- [ ] Replace the custom JWT bearer setup in `AkGaming.Identity/Api/Program.cs` with OpenIddict validation for new tokens.
- [ ] Keep the current legacy JWT path only as long as existing clients still need it.
- [ ] Add `AddControllersWithViews()` and `AddRazorPages()` to support server-rendered UI and endpoint pass-through.
- [ ] Add status code pages integration for OpenIddict errors.

## Phase 3: Introduce OpenIddict Protocol Endpoints

- [ ] Add an authorization controller for `/connect/authorize`.
- [ ] Add a token endpoint handler for `/connect/token`.
- [ ] Add a userinfo endpoint handler for `/connect/userinfo`.
- [ ] Add an end-session endpoint handler for `/connect/logout`.
- [ ] Enable authorization endpoint pass-through.
- [ ] Enable token endpoint pass-through only where custom exchange logic is required.
- [ ] Enable logout/end-session pass-through if custom logout UX is required.
- [ ] Keep OpenIddict token storage enabled.
- [ ] Configure Authorization Code flow.
- [ ] Configure Refresh Token flow.
- [ ] Require PKCE globally.
- [ ] Register standard scopes: `openid`, `profile`, `email`, `roles`.
- [ ] Register custom API scopes separately, e.g. `management_api`.

## Phase 4: Seed OpenIddict Clients and Scopes

- [ ] Add a startup seeder for OpenIddict applications in `AkGaming.Identity/Infrastructure`.
- [ ] Add a startup seeder for OpenIddict scopes in `AkGaming.Identity/Infrastructure`.
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
- [ ] Use explicit consent for third-party clients by default.

## Phase 5: Move UI from Static HTML to Server-Rendered Pages

- [ ] Replace `AkGaming.Identity/Api/wwwroot/ui/login.html` with a Razor Page or MVC view.
- [ ] Replace `AkGaming.Identity/Api/wwwroot/ui/register.html` with a Razor Page or MVC view.
- [ ] Replace `AkGaming.Identity/Api/wwwroot/ui/index.html` with an account/manage page.
- [ ] Replace the current callback/token-fragment handling page with normal server-side login and authorization resumption.
- [ ] Keep existing CSS/assets from `wwwroot/ui` where useful.
- [ ] Remove browser-side token storage from the Identity site's own UI.
- [ ] Stop using `localStorage` for access/refresh tokens in Identity-hosted pages.
- [ ] Add pages for:
  - [ ] login
  - [ ] register
  - [ ] consent
  - [ ] logout
  - [ ] account/manage
  - [ ] error

## Phase 6: Split Authentication Logic from Token Issuance

- [ ] Refactor `AkGaming.Identity/Application/Auth/AuthService.cs` so password login no longer directly issues JWT/refresh tokens for the new path.
- [ ] Keep password validation, lockout, email verification, default role assignment, and audit logging logic.
- [ ] Introduce a local sign-in service or equivalent application-layer abstraction for issuing the Identity-site cookie.
- [ ] Introduce an OpenIddict principal factory that converts `User` plus granted scopes into a `ClaimsPrincipal`.
- [ ] Implement explicit claim destinations for access tokens and ID tokens.
- [ ] Stop reusing the legacy `JwtTokenService` for the new OIDC issuance path.
- [ ] Stop reusing the legacy custom refresh token store for the new OIDC issuance path.

## Phase 7: Rework Authorization Request Handling

- [ ] In `/connect/authorize`, if the user is not authenticated locally, challenge to the local login page and preserve the original authorization request.
- [ ] After local sign-in, resume the authorization request.
- [ ] Load the requesting client from OpenIddict application storage.
- [ ] Validate requested scopes and consent requirements.
- [ ] Create permanent OpenIddict authorizations for remembered consent.
- [ ] Return `Forbid(...)` with OpenIddict error properties for protocol-level errors like `consent_required`.
- [ ] Use `prompt=none`, `prompt=login`, and consent behavior correctly.

## Phase 8: Preserve and Rework Discord Flow

- [ ] Keep `AkGaming.Identity/Domain/Entities/ExternalLogin.cs` unchanged as the source of linked Discord identities.
- [ ] Keep account resolution rules in `AkGaming.Identity/Application/Auth/AuthService.cs`:
  - [ ] existing Discord link logs the user into the matching account
  - [ ] existing email plus manual-link requirement blocks auto-link
  - [ ] optional auto-create creates a local account and then links Discord
- [ ] Change Discord callback behavior so it signs the user into the local cookie instead of returning browser tokens.
- [ ] Keep Discord link flow for already authenticated local users.
- [ ] Update `DiscordOAuthState` usage so it stores local return-path / authorization-resume information, not legacy redirect-bridge token state.
- [ ] Keep audit logging for Discord login/link success and failure.

## Phase 9: Update Identity APIs to the New Validation Path

- [ ] Move new bearer validation for admin/account APIs to OpenIddict validation.
- [ ] Ensure `/auth/me` or its replacement account endpoint works with the new tokens while the UI is being migrated.
- [ ] Review role claims and authorization policies against the new claim format.
- [ ] If needed, add a custom API scope such as `management_api` and require it where appropriate.
- [ ] Keep legacy auth endpoints temporarily for backward compatibility, but do not extend them further.

## Phase 10: Deprecate Legacy Browser Token Bridge

- [ ] Mark these endpoints as transitional:
  - [ ] `/auth/login`
  - [ ] `/auth/register`
  - [ ] `/auth/refresh`
  - [ ] `/auth/logout`
  - [ ] `/auth/redirect/finalize`
- [ ] Stop sending tokens in URL fragments.
- [ ] Stop treating `Bridge:AllowedRedirectUris` as the client registry.
- [ ] Stop storing refresh tokens in browser-managed storage for Identity-hosted UI.
- [ ] Remove the legacy bridge flow only after all clients have migrated to `/connect/*`.

## Tests

- [ ] Update `AkGaming.Identity/Tests/Api.IntegrationTests/TestApiFactory.cs` to seed OpenIddict clients/scopes for tests.
- [ ] Add integration tests for unauthenticated `/connect/authorize` redirecting to login.
- [ ] Add integration tests for successful authorization code flow with PKCE.
- [ ] Add integration tests for token exchange at `/connect/token`.
- [ ] Add integration tests for refresh token issuance and redemption.
- [ ] Add integration tests for `/connect/userinfo`.
- [ ] Add integration tests for consent persistence and repeated authorizations.
- [ ] Add integration tests for logout/end-session behavior.
- [ ] Add integration tests for Discord login under the new local-cookie flow.
- [ ] Add integration tests for Discord linking while already locally authenticated.
- [ ] Add unit tests for the new OpenIddict principal factory and claim destination logic.

## Suggested PR Breakdown

### PR 1: OpenIddict Foundation

- [ ] Add packages.
- [ ] Add OpenIddict DB tables/migration.
- [ ] Add application/scope seeding.
- [ ] Register OpenIddict core/server/validation in the host.

### PR 2: Local Cookie + UI Migration

- [ ] Add local cookie auth.
- [ ] Replace static login/register/account pages with Razor Pages or MVC views.
- [ ] Add error page integration.

### PR 3: `/connect/*` Endpoints

- [ ] Implement authorization, token, userinfo, and logout endpoints.
- [ ] Add consent handling and authorization persistence.
- [ ] Enforce Authorization Code + PKCE.

### PR 4: Discord Integration Rewrite

- [ ] Rework Discord callback to sign in locally.
- [ ] Rework link flow to operate on local authenticated session.
- [ ] Preserve all current account resolution behavior.

### PR 5: Validation + Legacy Deprecation

- [ ] Move APIs to OpenIddict validation.
- [ ] Keep temporary legacy compatibility.
- [ ] Remove old bridge code only after client migration.

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
- `AkGaming.Identity/Infrastructure/Persistence/Migrations/*`
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
- Require PKCE.
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
