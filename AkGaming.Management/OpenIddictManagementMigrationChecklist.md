# OpenIddict Management Tool Migration Checklist

This checklist covers migrating `AkGaming.Management` from the current custom token bridge to the new OpenIddict-based Identity provider.

Scope:
- Management frontend (`AkGaming.Management/Frontend`)
- Management Web API (`AkGaming.Management/WebApi`)
- Keep the existing management UI routes where practical
- Use interactive Authorization Code flow
- Reuse the Identity provider's new `/connect/*` endpoints

Constraints:
- The management frontend is a server-rendered ASP.NET Core app with interactive server components, not a SPA.
- The management frontend currently calls both:
  - the management Web API
  - Identity admin endpoints via `IdentityApiClient`
- The current frontend must stop parsing and trusting raw JWTs at its callback route.
- The current Web API must stop depending on the shared JWT secret as its primary validation model.

## Current Findings

- `AkGaming.Management/Frontend/Startup/WebApplicationExtensions.cs`
  - builds custom login/register redirects against Identity
  - receives tokens at `/authentication/callback`
  - parses the `access_token` locally with `JwtSecurityTokenHandler.ReadJwtToken(...)`
  - creates the auth cookie from unvalidated token contents
- `AkGaming.Management/Frontend/Handlers/ApiAuthorizationHandler.cs`
  - reads `access_token`/`refresh_token` from the cookie auth properties
  - performs custom refresh calls against `/auth/refresh`
  - sends non-standard JSON payloads instead of OAuth 2.0 token requests
- `AkGaming.Management/Frontend/appsettings*.json`
  - points at `/login`, `/register`, `/auth/refresh`, `/auth/logout`, `/auth/me`
  - does not have OIDC discovery/client settings yet
- `AkGaming.Management/WebApi/Startup/ServiceCollectionExtensions.cs`
  - uses `AddJwtBearer(...)`
  - still supports shared-secret validation via `Jwt:SecretKey`
  - has no OpenIddict validation or scope checks
- `AkGaming.Management/Frontend/AkGaming.Management.Frontend.csproj`
  - already references `Microsoft.AspNetCore.Authentication.OpenIdConnect`
  - does not actually use the OIDC handler yet

## Target Architecture

- The management frontend becomes a standard ASP.NET Core confidential OIDC client:
  - local cookie session
  - OpenID Connect challenge/sign-in
  - Authorization Code flow with PKCE
  - refresh tokens handled through the standard `/connect/token` endpoint
- The management Web API validates access tokens issued by the Identity server using OpenIddict validation.
- The management frontend keeps `/authentication/login` and `/authentication/logout` as stable app routes, but these become thin wrappers around ASP.NET Core challenge/sign-out behavior.
- The custom `/authentication/callback` bridge is removed and replaced by the standard OIDC callback endpoints.
- The management frontend requests scopes deliberately:
  - `openid`
  - `profile`
  - `email`
  - `roles`
  - `offline_access`
  - `management_api` (or a refined split such as `management_api` + `identity_admin_api`)

## Phase 1: Register the Management Client in Identity

- [x] Add a real OpenIddict application entry for the management frontend in `AkGaming.Identity`.
- [x] Use a confidential client because the management frontend is server-side.
- [x] Define:
  - [x] `client_id`
  - [x] `client_secret`
  - [x] redirect URI for sign-in callback
  - [x] post-logout redirect URI
  - [x] endpoint permissions
  - [x] grant type permissions
  - [x] response type permissions
  - [x] scope permissions
  - [x] PKCE requirement
- [x] Decide the access-token scope model for the management suite:
  - [x] one shared API scope such as `management_api`
  - [ ] or separate scopes/resources for Identity admin APIs and the management Web API
- [x] Keep consent implicit only if this client is considered fully first-party/trusted.

## Phase 2: Replace Frontend Custom Auth Bridge with OIDC

- [x] Replace the custom auth wiring in `AkGaming.Management/Frontend/Startup/ServiceCollectionExtensions.cs` with:
  - [x] cookie authentication as the default scheme
  - [x] OpenID Connect as the challenge scheme
- [x] Configure OIDC options in the frontend:
  - [x] authority/issuer from the Identity server
  - [x] client id
  - [x] client secret
  - [x] response type `code`
  - [x] `UsePkce = true`
  - [x] `SaveTokens = true`
  - [x] requested scopes
  - [x] signed-out callback path
  - [x] claim type mapping for name/roles
- [x] Use discovery metadata instead of hardcoded auth endpoint paths where possible.
- [x] Stop parsing JWTs manually in the frontend callback path.
- [x] Stop creating auth cookies from unvalidated token payloads.

## Phase 3: Replace `/authentication/*` Endpoints with Thin Wrappers

- [x] Remove the custom HTML/token-posting callback in `AkGaming.Management/Frontend/Startup/WebApplicationExtensions.cs`.
- [x] Replace `/authentication/login` with `Challenge(...)` against the OIDC scheme.
- [x] Preserve management `returnUrl` handling, but let ASP.NET Core/OpenID Connect own `state`, nonce, and correlation cookies.
- [x] Replace `/authentication/logout` with `SignOut(...)` using:
  - [x] the local cookie scheme
  - [x] the OIDC sign-out flow
- [x] Use standard callback paths such as:
  - [x] sign-in callback (`/signin-oidc` or equivalent)
  - [x] sign-out callback (`/signout-callback-oidc` or equivalent)
- [x] Decide what to do with `/authentication/register`:
  - [ ] either remove it if unused
  - [ ] or keep it as a redirect to the Identity register page, with a return path back into the OIDC login flow
  - [x] keep it as a compatibility alias to the OIDC login entry point and let registration happen on the Identity login/register pages

## Phase 4: Rework Frontend Token Refresh

- [x] Keep token storage server-side in the authentication cookie properties via `SaveTokens`.
- [x] Rework `AkGaming.Management/Frontend/Handlers/ApiAuthorizationHandler.cs` to refresh via standard OAuth 2.0 token exchange:
  - [x] `POST /connect/token`
  - [x] `grant_type=refresh_token`
  - [x] `refresh_token=...`
  - [x] `client_id=...`
  - [x] `client_secret=...` for the confidential client
- [x] Remove the custom `/auth/refresh` JSON payload shape.
- [x] Read the standard token response fields:
  - [x] `access_token`
  - [x] `refresh_token`
  - [x] `expires_in`
- [x] Preserve the current "refresh slightly before expiry" behavior.
- [ ] If refresh fails, sign out the local session and redirect back through normal OIDC login.

## Phase 5: Claims and User Identity in the Frontend

- [ ] Verify role claims still satisfy all `[Authorize(Roles = "Admin")]` usage in the frontend.
- [x] Ensure `ClaimTypes.NameIdentifier` is available from `sub`.
- [x] Ensure the displayed name remains stable:
  - [x] email is acceptable as the display name for now
  - [ ] or map a richer profile claim later
- [x] Decide whether to enrich claims from the userinfo endpoint or rely on token claims only.
- [x] Remove any fallback logic that assumes raw JWT parsing is available in the frontend.

## Phase 6: Migrate the Management Web API to OpenIddict Validation

- [x] Replace `AddJwtBearer(...)` in `AkGaming.Management/WebApi/Startup/ServiceCollectionExtensions.cs` with OpenIddict validation.
- [x] Because the Web API is hosted in a different project than the Identity server, configure:
  - [x] issuer/discovery
  - [x] `UseSystemNetHttp()`
  - [x] ASP.NET Core host integration
- [x] Disable Identity access-token encryption for the remote management API scenario, so shared decryption credentials are no longer required.
- [x] Stop treating `Jwt:SecretKey` as the primary validation mechanism.
- [x] Keep role-based policies, but evaluate them under the new claims principal produced by OpenIddict validation.
- [x] Add an explicit scope policy for management API access, e.g. `management_api`.
- [x] Apply that scope requirement to the management API endpoints instead of relying on roles alone.

## Phase 7: Align Identity Admin API Access

- [x] Decide whether Identity admin endpoints should also require a scope in addition to `Admin` role.
- [x] If yes, add the scope requirement in `AkGaming.Identity/Api/Endpoints/AdminEndpoints.cs`.
- [x] Ensure the management frontend requests the scope needed to call:
  - [x] Identity admin APIs
  - [x] management module APIs
- [ ] Avoid over-broad tokens if the frontend does not need all admin surfaces in one session.

## Phase 8: Frontend UX Cleanup

- [x] Keep `LoginDisplay`, `UnauthorizedPanel`, and existing navigation links working by preserving `/authentication/login` and `/authentication/logout` routes.
- [x] Remove the now-obsolete `/authentication/callback` bridge page.
- [ ] Update any components that force logout on generic `401` results to work cleanly with the new refresh/sign-out behavior.
- [ ] Review `AccessDenied` and unauthorized flows so they redirect to the right login/logout routes.
- [x] Remove any debug-only token decoding paths that depend on direct JWT parsing unless they are explicitly kept for diagnostics.

## Phase 9: Configuration Cleanup

- [x] Replace current frontend `Auth:*` settings that point to legacy endpoints with OIDC client settings, for example:
  - [x] authority
  - [x] client id
  - [x] client secret
  - [x] callback path
  - [x] signed-out callback path
  - [x] requested scopes
- [x] Keep only the settings that are still meaningful after OIDC migration.
- [x] Remove obsolete settings such as:
  - [x] `RefreshPath`
  - [x] `RefreshEndpoint` if no longer needed separately
  - [x] `LoginRedirectUriParam`
  - [x] `LoginStateParam`
  - [x] `LogoutReturnUrlParam` if OIDC sign-out options replace it
- [x] Update management deployment configuration so frontend and Web API can resolve the Identity discovery endpoint correctly.
- [ ] Update production secrets/config for the confidential client secret.
- [x] Shared encryption credentials are no longer needed after disabling Identity access-token encryption for this remote API scenario.

## Phase 10: Tests

- [ ] Add frontend integration tests for:
  - [ ] unauthenticated request redirects to OIDC challenge
  - [ ] successful sign-in roundtrip
  - [ ] `returnUrl` preservation
  - [ ] logout/end-session roundtrip
  - [ ] token refresh via `/connect/token`
  - [ ] refresh failure signs the user out
- [ ] Add Web API integration tests for:
  - [ ] valid access token acceptance
  - [ ] missing scope rejection
  - [ ] admin-role enforcement
  - [ ] self-route policy with `sub` / `NameIdentifier`
- [ ] Add an end-to-end smoke test covering:
  - [ ] management login
  - [ ] Identity admin page access
  - [ ] member page access
  - [ ] logout

## Suggested PR Breakdown

### PR 1: Frontend OIDC Foundation

- [x] Add OIDC configuration and schemes
- [x] Keep `/authentication/login` and `/authentication/logout` as wrappers
- [x] Remove custom callback token bridge

### PR 2: Standards-Based Token Refresh

- [x] Rewrite `ApiAuthorizationHandler` to use `/connect/token`
- [x] Persist refreshed tokens in the auth cookie
- [ ] Handle refresh failure cleanly

### PR 3: Web API Validation Migration

- [x] Switch the Web API to OpenIddict validation
- [x] Add scope-based authorization
- [x] Remove shared-secret-first validation assumptions

### PR 4: Cleanup and Rollout

- [x] Clean up obsolete frontend auth config
- [ ] Add missing tests
- [x] Remove compatibility code that only existed for the custom bridge

## Files Expected to Change

- `AkGaming.Management/Frontend/Program.cs`
- `AkGaming.Management/Frontend/Startup/ServiceCollectionExtensions.cs`
- `AkGaming.Management/Frontend/Startup/WebApplicationExtensions.cs`
- `AkGaming.Management/Frontend/Handlers/ApiAuthorizationHandler.cs`
- `AkGaming.Management/Frontend/appsettings.json`
- `AkGaming.Management/Frontend/appsettings.Development.json`
- `AkGaming.Management/Frontend.Components/Auth/*`
- `AkGaming.Management/WebApi/Startup/ServiceCollectionExtensions.cs`
- `AkGaming.Management/WebApi/Program.cs`
- `AkGaming.Management/WebApi/appsettings*.json`
- `AkGaming.Management/Frontend/*Tests*`
- `AkGaming.Management/WebApi/*Tests*`

## External Dependency on Identity

This migration depends on the Identity server exposing a stable OpenIddict client registration for the management frontend and stable scope enforcement for the management APIs.

Decision taken during implementation:
- the Identity server now emits non-encrypted access tokens for the management remote API scenario
- the management Web API validates them via OpenIddict validation and discovery

## Reference Documentation

- https://documentation.openiddict.com/guides/choosing-the-right-flow
- https://documentation.openiddict.com/integrations/aspnet-core
- https://documentation.openiddict.com/guides/getting-started/implementing-token-validation-in-your-apis
- https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.openidconnect.openidconnectoptions.usepkce
