# GamelyBot

GamelyBot is the private Discord integration gateway for AK Gaming applications. It accepts semantic notification events, stores them durably, renders Discord messages centrally, and delivers them to the configured club server and linked applicants.

## Current notification flows

- `reimbursement.submitted`: mentions the treasurer role in the administration channel and confirms submission by DM when the applicant linked Discord.
- `reimbursement.status-changed`: sends the applicant a DM for review, approval, rejection, payment, or cancellation changes.
- Board meeting lifecycle, agenda, and reminder events: notify the board channel. Created, rescheduled, and reminder messages include availability and rescheduling controls. GamelyBot automatically queues one reminder when the next meeting enters the configured one-hour window.

Board members with a linked Discord account can also use the guild-scoped `/boardmeeting` commands to view the next meeting and backlog, set availability, send a manual reminder, add agenda or backlog items, and promote backlog items to the next agenda. `/boardmeeting create` links to the management tool, where meetings and their initial agendas are created. Add commands use Discord popup forms, and backlog selection uses autocomplete.

An unavailable or blocked DM is recorded independently and never rolls back the originating reimbursement operation.

## Local development without Discord

Development defaults to the debug transport and disables service authentication only in the Development environment. No Discord token or running Identity service is required.

```bash
dotnet run --project AkGaming.GamelyBot/AkGaming.GamelyBot.csproj --urls http://localhost:5088
```

The included `http` launch profile sets `ASPNETCORE_ENVIRONMENT=Development`. If launch profiles are disabled, set the environment explicitly before starting the service.

Use `AkGaming.GamelyBot.http` to submit a sample notification. Rendered channel messages and DMs are written to structured logs and can be inspected at:

```text
GET http://localhost:5088/api/debug/deliveries
```

The Management development configuration dispatches its transactional outbox to this local endpoint. Set `Notifications__Endpoint` to an empty value when deliberately developing Management without the bot.

## Discord application setup

Create separate Discord applications/bot tokens for test and production so both environments can run concurrently while each bot belongs to exactly one server.

For each Discord application:

1. Enable only the Guild Install context.
2. Set the public install link to `None`.
3. Install it manually into the corresponding test or club server.
4. Include the `bot` and `applications.commands` scopes during installation.
5. Grant only View Channels, Send Messages, and Embed Links in the administration and board channels.
6. Do not enable the Message Content intent; outbound notifications and slash commands do not need it.
7. Set the Interactions Endpoint URL to `https://<gamelybot-host>/api/discord/interactions`.
8. Configure the immutable guild, administration-channel, board-channel, treasurer-role, board-role IDs, and the application's public key.

When the Discord transport is enabled, GamelyBot creates or updates its guild-scoped `/boardmeeting` command during startup and removes the legacy `/board` command. Existing unrelated guild commands are left unchanged.

Startup fails if the bot is installed in another server or if the configured channel/role does not belong to the configured server. Applicant DMs are attempted only after verifying that the linked Discord user belongs to that server. Role mentions use an explicit allowed-role list, so notification payloads cannot introduce arbitrary mentions.

## Configuration

Production and test deployments should use environment variables or secret storage:

```text
Database__Provider=Postgres
ConnectionStrings__DefaultConnection=Host=...;Database=...;Username=...;Password=...
OpenIddictValidation__Issuer=https://identity.example/
NotificationTransport=discord
Discord__Token=...
Discord__GuildId=...
Discord__AdministrationChannelId=...
Discord__TreasurerRoleId=...
Discord__BoardChannelId=...
Discord__BoardRoleId=...
Discord__ApplicationPublicKey=...
IdentityClient__BaseUrl=https://identity.example/
IdentityClient__TokenEndpoint=https://identity.example/connect/token
IdentityClient__ClientId=akgaming-gamelybot
IdentityClient__ClientSecret=...
IdentityClient__Scope=identity_discord_links management_board_interactions
ManagementClient__BaseUrl=https://management.example/api/
ManagementClient__BoardMeetingsUrl=https://management.example/board/meetings
DiscordInteractions__EnableAutomaticReminders=true
DiscordInteractions__ReminderLeadTimeMinutes=60
DiscordInteractions__ReminderPollIntervalSeconds=60
```

`ManagementClient__BoardMeetingsUrl` is optional when the frontend is hosted beside the API; otherwise GamelyBot derives `/board/meetings` from `ManagementClient__BaseUrl`.

Management needs:

```text
Notifications__Endpoint=https://gamelybot.example/api/notifications
Notifications__TokenEndpoint=https://identity.example/connect/token
Notifications__ClientId=akgaming-management-api
Notifications__ClientSecret=...
Notifications__Scope=gamelybot_notifications
Notifications__ManagementBaseUrl=https://management.example
```

Identity must seed two confidential clients with client-credentials enabled:

- `akgaming-management-api`, allowed `gamelybot_notifications`
- `akgaming-gamelybot`, allowed `identity_discord_links` and `management_board_interactions`

Configure these entries through `OpenIddict__Applications__<index>__...`; never commit their production secrets. The development registrations and secrets are local-only examples in `appsettings.Development.json`.

## Reliability model

Management writes the reimbursement and its outbox message in the same database transaction. A background dispatcher retries submission until the bot durably accepts the event. The bot deduplicates by event ID, stores separate channel/DM delivery records, and retries transient delivery failures with exponential backoff.

Discord does not offer an idempotency key for message creation, so a process crash after Discord accepts a message but before the local delivery record is committed can rarely produce a duplicate. Normal retries and repeated producer submissions remain idempotent.

Run exactly one bot-service replica per environment. The worker recovers notifications left in `processing` after a restart, but active-active delivery workers are intentionally not part of this first version.

## Deployment migrations

The GamelyBot deployment workflow applies PostgreSQL migrations before triggering Coolify. Configure these repository secrets with raw Npgsql connection strings:

- `GAMELYBOT_TEST_DB_CONNECTION_STRING`
- `GAMELYBOT_PRODUCTION_DB_CONNECTION_STRING`

The workflow uses the shared `DB_SSH_*` tunnel secrets when present. In that case, the connection strings must target the workflow's local tunnel port, matching the Identity and Management deployment convention.

SQLite and PostgreSQL use separate migration projects because their EF models contain provider-specific column types. When the persistence model changes, add the same migration to both providers:

```bash
dotnet ef migrations add <MigrationName> --project AkGaming.GamelyBot/Migrations/Sqlite/AkGaming.GamelyBot.Migrations.Sqlite.csproj --startup-project AkGaming.GamelyBot/Migrations/Sqlite/AkGaming.GamelyBot.Migrations.Sqlite.csproj --context GamelyBotDbContext
dotnet ef migrations add <MigrationName> --project AkGaming.GamelyBot/Migrations/Postgres/AkGaming.GamelyBot.Migrations.Postgres.csproj --startup-project AkGaming.GamelyBot/Migrations/Postgres/AkGaming.GamelyBot.Migrations.Postgres.csproj --context GamelyBotDbContext
```
