# AkGaming.Gamenight

Standalone Game Night registration and admission application. The public site is
`https://gamenight.akgaming.de`. The server owns all data and permission checks.

## Projects

- `Contracts`: API requests, responses, form validation, and permission names.
- `Domain`: events, the global active-event selection, registrations, audit entries, and email outbox.
- `Application`: signup, verified-email ownership, deadlines, admission pricing, and front-desk state transitions.
- `Infrastructure`: EF persistence, Management/GamelyBot clients, and durable guest confirmation email.
- `Migrations/Sqlite` and `Migrations/Postgres`: independent provider migrations.
- `Shared`: Razor screens, API client, host-independent session interface, Core theme and components.
- `Frontend`: Blazor interactive-server host with OIDC cookies and server-side token storage/refresh.
- `Mobile`: MAUI Blazor Hybrid host with system-browser OIDC + PKCE and secure token storage.
- `Tests`: application behavior, SQLite persistence/Management queries, and controller tests.

The web and MAUI hosts render the same screens. Server-only Core cookie helpers
are compiled in `AkGaming.Core.Authentication`; `AkGaming.Core.Components` no
longer requires an ASP.NET server runtime. The server solution excludes the MAUI
host so Linux API deployment does not require mobile workloads.

## Features and operational rules

The signup preserves the 18 questions in the supplied Google Form PDF, including
Karaoke-only routing, all acknowledgment fields, food interests, and discovery
options. Quantity questions remain optional as in the PDF. Hidden Game Night
answers are removed for Karaoke-only registrations. Historical event text is
replaced by editable event information and deadlines.

Guests receive a private, read-only email link. The raw token is never returned
by the submission API; its hash is stored on the registration. The email body is
protected with ASP.NET Data Protection until delivery. The link expires seven
days after the event and is invalidated when ownership is claimed.

Signing in with the same verified email claims an unowned registration for the
active event. Afterwards the stable Identity subject owns the registration,
independently of future email changes. The API verifies ownership. An unverified
or unrelated account cannot claim, edit, or cancel another person's registration.

There is one registration per normalized email per event, including cancelled
registrations. Duplicate public submissions return the same generic receipt and
do not resend mail. There is no import or historical operations screen.

Admins create events and activate one globally. A Management payment period must
be selected before activation. UTC timestamps are stored; event inputs and public
event times use Europe/Berlin. Every operational write checks the active event
and participates in its concurrency token, preventing stale forms from writing
to an event after a switch.

Visitors can edit/cancel until the event's edit deadline, and never after
check-in. Staff may correct details with the registration-management permission.
Tariff-affecting changes after payment/check-in require reversing those states
first. Cancellation does not automatically refund or erase recorded payments.
There is no online payment processing.

Front Desk provides name/email search, status filters, admission details,
food/setup totals, CSV export, and permission-controlled context-menu actions.
Staff approval, registration, payment, and check-in are separate states.
The form's organizer declaration does not grant staff status or free admission.
Mutations record their actor, event, registration, action, and timestamp.

Membership eligibility comes from Management's narrowly scoped internal API:
an active Member, HonoraryMember, or SupportingMember linked to the Identity
subject must have a `Paid` due for the selected period. Pending, cancelled,
waived, absent, or older dues do not qualify. No membership financial details
are exposed. A failed lookup leaves the price unresolved and prevents payment
collection until it can be verified. Payment/check-in snapshots the admission
price so subsequent configuration changes do not rewrite a collected amount.

## Permissions

Assign these through Identity's existing role/permission administration:

| Permission | Purpose |
| --- | --- |
| `gamenight.events.manage` | Create/configure events and select the active one |
| `gamenight.registrations.read` | Search/read front-desk registrations |
| `gamenight.registrations.manage` | Edit/cancel visitors' registrations |
| `gamenight.registrations.export` | Export CSV; also requires registration read |
| `gamenight.admission.manage` | Approve/remove event staff status |
| `gamenight.frontdesk.manage` | Record/reverse payments and check-in |

Give desk operators read plus frontdesk. Grant admission and registration
management only where needed. Visitor self-service uses ownership instead.

## Local development

Use .NET 10. Start Identity and Management using their existing HTTPS profiles.
The Gamenight development clients are seeded in Identity configuration.

```sh
dotnet run --project AkGaming.Gamenight/WebApi
dotnet run --project AkGaming.Gamenight/Frontend
```

API: `http://localhost:5096`. Web: `https://localhost:7296`.
Identity: `https://localhost:7288`. Management: `https://localhost:3014/`.
Development configuration enables `Dev:AllowUntrustedLocalCertificates` for
localhost HTTPS calls (OIDC discovery/token exchange, refresh, API validation,
and Management/service requests). Only an untrusted certificate root is accepted;
hostname mismatches, expired certificates, and non-local hosts remain rejected.
The setting is ignored outside Development. Set it to false when using trusted
local certificates. Browsers still require trusting the local HTTPS certificate.

SQLite migrations run automatically in Development. SMTP defaults to disabled,
so email remains queued during development. Set SMTP configuration below to test
guest links; tokens are deliberately not logged.

```sh
dotnet test AkGaming.Gamenight/Tests -m:1
dotnet build AkGaming.Gamenight/AkGaming.Gamenight.slnx -m:1
dotnet workload install maui-android
dotnet build AkGaming.Gamenight/Mobile -f net10.0-android -m:1 -p:MSBuildEnableWorkloadResolver=true
```

Android requires the Android SDK and JDK 21. The mobile host currently points to
production Identity and Gamenight; change the endpoint settings in
`MauiProgram.cs`/`MobileSession.cs` for device testing against another deployment.
The iOS target is enabled on macOS and requires the iOS workload/Xcode and signing.
Mobile store packaging/signing is not included. Mobile logout clears the local
session; an Identity browser session can remain signed in.

## Deployment

Build both Dockerfiles from the monorepo root. Configure Coolify:

- Route `https://gamenight.akgaming.de/api/*` to the API container without removing
  `/api`; route the rest to the frontend.
- Set frontend `Api__BaseUrl` to the API's internal root URL, e.g.
  `http://gamenight-api:8080/`.
- Forward HTTPS scheme through the trusted reverse proxy. Do not expose the
  frontend container directly; it enables forwarded headers for that proxy.
- Set `AllowedHosts` for additional test domains.
- Run one API instance for the email dispatcher and one web instance for the
  in-memory authentication ticket store. Restarts require web users to sign in
  again. Multiple web instances need a shared ticket cache.
- Persist `/app/keys` separately on both containers. API keys are required to
  decrypt queued guest emails after a restart.
- Configure PostgreSQL and run migrations before starting a production deployment.

API environment:

```text
Database__Provider=Postgres
ConnectionStrings__Gamenight=Host=...;Database=...;Username=...;Password=...
Identity__Authority=https://identity.akgaming.de
App__PublicBaseUrl=https://gamenight.akgaming.de
Management__BaseUrl=https://management.akgaming.de/api/
Management__TokenEndpoint=https://identity.akgaming.de/connect/token
Management__ClientId=akgaming-gamenight-api
Management__ClientSecret=...
Management__Scope=management_gamenight_membership
Smtp__Enabled=true
Smtp__Host=...
Smtp__Port=587
Smtp__UseSsl=true
Smtp__Username=...
Smtp__Password=...
Smtp__FromEmail=no-reply@akgaming.de
```

Web environment:

```text
Identity__Authority=https://identity.akgaming.de
Identity__ClientSecret=...
Api__BaseUrl=http://gamenight-api:8080/
```

Identity seeds `gamenight_api` and `management_gamenight_membership` scopes.
New production client entries are appended after the existing Management client:

- index 1: `akgaming-gamenight-web`, confidential authorization code + PKCE.
  Configure `OpenIddict__Applications__1__ClientSecret`.
- index 2: `akgaming-gamenight-api`, confidential service client.
  Configure `OpenIddict__Applications__2__ClientSecret`.
- index 3: `akgaming-gamenight-mobile`, public authorization code + PKCE,
  callback `de.akgaming.gamenight://callback`; never assign a client secret.

If deployment configuration already overrides application indexes, append these
clients at unused indexes instead. Configure matching secrets before deploying
Identity. Test deployments need their own matching issuer, callback URLs,
service clients, and database. These scopes/seeded clients follow Identity's
existing protected-configuration behavior.

Deploy Identity and Management with their new contracts/endpoint before using
Gamenight's member pricing.

### CI/CD

`deploy-gamenight.yml` follows the existing workflow: relevant `develop`
changes deploy to test; `gamenight/*` tags deploy to production. Tests and web
build run before deployment. PostgreSQL migrations use the existing optional
SSH tunnel settings. Configure:

- `GAMENIGHT_TEST_DB_CONNECTION_STRING`, `GAMENIGHT_PRODUCTION_DB_CONNECTION_STRING`
- `COOLIFY_WEBHOOK_GAMENIGHT_BACKEND_TEST`, `COOLIFY_WEBHOOK_GAMENIGHT_FRONTEND_TEST`
- `COOLIFY_WEBHOOK_GAMENIGHT_BACKEND`, `COOLIFY_WEBHOOK_GAMENIGHT_FRONTEND`
- existing `COOLIFY_TOKEN` and optional `DB_SSH_*`/`DB_TUNNEL_TARGET_*`

Raw Npgsql connection strings are required. With SSH tunnelling, the test
connection targets localhost:55432 and production localhost:55433.
`check-gamenight.yml` additionally builds Android on PRs and manual dispatch.

Add future migrations to both providers:

```sh
dotnet ef migrations add <Name> --project AkGaming.Gamenight/Migrations/Sqlite --startup-project AkGaming.Gamenight/Migrations/Sqlite
dotnet ef migrations add <Name> --project AkGaming.Gamenight/Migrations/Postgres --startup-project AkGaming.Gamenight/Migrations/Postgres
```

## Future modules

Food answers are signup preferences, not orders. A later ordering module should
reference `EventId` and `RegistrationId`, own menus/order lines/payment/collection,
and consume signup preferences explicitly. It should not overwrite the original
registration answers or repurpose entry-payment fields.

`GamelyBotConnection` is registered with service-token support.
`GamelyBot:Enabled` defaults to false; signup sends no bot events. Configure its
endpoint/client secret when introducing notification flows, then add durable
events through the existing Core notification pattern.

## Validation limits

Automated tests cover ownership, deadlines, free admission, member-price
calculation, payment/check-in locking, concurrent writes, active-event switching,
and SQLite Management queries. Browser smoke checks cover guest submission,
Karaoke branching and narrow-screen overflow. Live production OIDC/SMTP/Coolify
connections require deployment secrets. Android is build-validated; iOS/device
runtime and store publication require their platform environments.
