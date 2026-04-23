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
- Teams own player profiles.
- Player profiles are scoped to a specific game.
- There is one player profile model for both registered users and guest players.
- A player profile can optionally be linked to an AK Gaming user account.
- The profile type distinguishes between:
  - `User`: backed by a real user account
  - `Guest`: created by a captain without a linked user account
- Guest player profiles exist so a captain can register a full team without waiting for every player to sign up first.

## Games
- `Game` is its own domain type.
- Tournaments are scoped to a game.
- Player profiles are also scoped to a game.
- This allows one team to maintain separate player profiles per game instead of pretending one profile works across all titles.
- Later validation should ensure that a roster for a tournament only contains player profiles for that tournament's game.

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
- Validate that a submitted roster only contains player profiles owned by the registering team.
- Validate that submitted player profiles belong to the same game as the tournament.
