# Tournament Domain Notes

This file captures domain and workflow decisions that are intentionally not fully implemented yet.

## Team Registration And Rosters
- A team does not directly become part of a tournament. It creates a tournament-specific registration.
- A tournament registration has an approval status and a currently active roster.
- A roster change is not an in-place edit. It is submitted as a new roster revision.
- While a new roster revision is pending review, the previously approved roster remains the active roster.
- If the new revision is approved, it replaces the active roster.
- If the new revision is rejected, the old active roster stays unchanged.

## Player Profiles
- Player profiles are scoped to a specific game.
- There is one player profile model for both registered users and guest players.
- A player profile can optionally be linked to an AK Gaming user account.
- User-backed player profiles are owned by the user, not by a team.
- Guest player profiles are owned by a team.
- The profile type distinguishes between:
  - `User`: backed by a real user account
  - `Guest`: created by a captain without a linked user account
- Guest player profiles exist so a captain can register a full team without waiting for every player to sign up first.

## Teams And Memberships
- Teams are not scoped to a single game.
- A team can have members with roles:
  - `Owner`
  - `Editor`
  - `Member`
- The creator of a team should become an owner automatically.
- The exact owner constraints should be enforced in the application layer. At minimum, a team should not end up without an owner.
- A team's available player pool for a game consists of:
  - guest player profiles owned by the team for that game
  - user-backed player profiles for team members for that game

## Games
- `Game` is its own domain type.
- Tournaments are scoped to a game.
- Player profiles are also scoped to a game.
- This allows one team to maintain separate player profiles per game instead of pretending one profile works across all titles.
- Later validation should ensure that a roster for a tournament only contains player profiles for that tournament's game.

## Logo Assets
- Games, tournaments, teams, and player profiles can all have a logo.
- Logos are modeled as references to a reusable `MediaAsset` entity instead of storing image bytes directly on the aggregate.
- Domain entities only keep `LogoAssetId` and `LogoAsset`.
- Replacing a logo should create or select a different media asset and update the aggregate reference.
- Roster snapshots currently do not copy logo information. They snapshot player identity data, not branding data.
- `MediaAsset` is intentionally storage-agnostic in the domain. Storage location, upload flow, and public URL generation belong to later layers.

## Roster Snapshots
- Rosters do not point to live player profile data only. Each roster stores snapshots of the selected player profiles.
- A snapshot contains the information needed for tournament participation even if the source player profile changes later.
- Snapshots should at least capture:
  - source player profile id
  - player profile type
  - linked user id if one exists
  - player name
  - source player profile revision timestamp
  - snapshot creation timestamp
- A roster snapshot can be marked as potentially outdated if the underlying player profile was revised after the snapshot was created.
- This outdated state is informational. It should not automatically invalidate the active roster.

## Editing Rules
- User-backed player profiles can be edited at any time.
- Guest player profiles can also be edited, but existing roster snapshots stay immutable.
- Editing a player profile does not rewrite past roster snapshots.
- If a player profile is changed after a snapshot was taken, the snapshot may become outdated and can later be reviewed or refreshed by a new roster submission.

## Removal Rules
- A player profile that is part of an active or pending roster should not be hard-deleted.
- Preferred implementation options:
  - block removal while the profile is referenced by an active or pending roster
  - or use soft deletion / deactivation instead of physical deletion
- Normal profile edits are allowed. Membership removal is the operation that needs protection.

## Later Application Layer Work
- Submit tournament registration with an initial roster.
- Approve or reject tournament registrations.
- Submit roster change requests for already approved registrations.
- Approve or reject pending roster revisions independently from the existing active roster.
- Surface whether a roster snapshot is potentially outdated compared to the current player profile.
- Validate that a submitted roster only contains guest player profiles owned by the registering team or user-backed player profiles belonging to team members.
- Validate that submitted player profiles belong to the same game as the tournament.
- Add member removal flows with owner safety rules.
- Add guest player profile removal flows that respect active and pending roster references.
- Add authorization for registration review actions once tournament administration roles are modeled.
- Add upload endpoints and application services for logo assets.
- Validate uploaded logos for file type, size, and image safety before persisting them.
- Persist `MediaAsset` metadata separately from binary storage.
- Introduce a storage abstraction so local disk can be used first and object storage can be added later.
- Add logo replacement and removal flows for games, tournaments, teams, and player profiles.
- Decide whether unused media assets are deleted immediately or cleaned up asynchronously once no aggregate references them anymore.
- Add image normalization / resizing rules for logo variants if the frontend needs multiple sizes.
