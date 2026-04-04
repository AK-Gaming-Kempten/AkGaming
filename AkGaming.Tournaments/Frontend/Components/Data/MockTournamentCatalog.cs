namespace AkGaming.Tournaments.Frontend.Components.Data;

public sealed class MockTournamentCatalog
{
    private readonly IReadOnlyList<TournamentDetail> _tournaments =
    [
        new(
            new TournamentSummary(
                "rift-rumble-summer-2026",
                "Rift Rumble",
                "Summer 2026",
                "League of Legends",
                "32 Team Swiss -> Top 8",
                "Remote / EUW",
                "Registration closes 15 May",
                "Featured",
                "positive",
                "AK Gaming's flagship Rift event with rank checks, captain onboarding, and matchday support.",
                24,
                32),
            "Built for the LoL tournament, flexible enough for everything after it.",
            "The first version focuses on tournament discovery, captain onboarding, and tournament spaces for public viewers, players, and admins.",
            "Captain briefing on 18 May, check-in on 24 May, first round on 25 May.",
            [
                new("Registered teams", "24 / 32", "A few late qualifier slots are still open."),
                new("Average MMR band", "Emerald 2", "Pulled from the mock rank-verification pass."),
                new("Check-in rate", "78%", "Based on invited player confirmations."),
                new("Operations load", "12 open items", "Enough to justify an admin desk view.")
            ],
            [
                new("Update", "Ruleset v0.4 staged", "Fair-play clause and pause policy are visible in the captain briefing area.", "3h ago", "positive"),
                new("Alert", "Top lane substitutions flagged", "Two teams still need replacement players linked to verified accounts.", "Yesterday", "warn"),
                new("Prep", "Broadcast block reserved", "Semi-finals can now be mirrored into a public stream overlay later on.", "2 days ago", "neutral")
            ],
            [
                new("Swiss Round 1", "25 May, 18:00", "Arena Alpha", "HSK Demon", "AKG Zamrichten", "#04 seed", "#07 seed", "Ready", "positive", "Map veto window opens 30 minutes before kickoff."),
                new("Swiss Round 1", "25 May, 18:00", "Arena Beta", "Campus Five", "Blue Orchid", "#09 seed", "#12 seed", "Pending lineups", "warn", "Support players have not accepted the invite flow yet."),
                new("Captain Scrim Block", "22 May, 20:00", "Practice Lobby", "Open Lobby A", "Open Lobby B", "Volunteer coached", "Observer ready", "Optional", "neutral", "Useful for testing role verification and Discord check-in.")
            ],
            [
                new("AKG Zamrichten", "South Germany", "Seed #07", "Captain onboarded, roster complete, one substitute pending final account link.", [
                    new("Kai", "Captain / Jungle", "Confirmed"),
                    new("Lena", "Mid", "Confirmed"),
                    new("Noah", "ADC", "Confirmed"),
                    new("Mira", "Support", "Invited"),
                    new("Sven", "Top", "Confirmed")
                ]),
                new("HSK Demon", "Campus Squad", "Seed #04", "Highest average MMR in the current sign-up pool, fully verified roster.", [
                    new("Zed", "Captain / Mid", "Confirmed"),
                    new("Vox", "Jungle", "Confirmed"),
                    new("Eri", "ADC", "Confirmed"),
                    new("Tara", "Support", "Confirmed"),
                    new("Mako", "Top", "Confirmed")
                ]),
                new("Blue Orchid", "Allgäu Mix", "Waitlist #01", "Team exists in the shared team database and is attached to two planned summer tournaments.", [
                    new("Ivy", "Captain / Support", "Confirmed"),
                    new("Rune", "Top", "Confirmed"),
                    new("Nox", "Mid", "Invited"),
                    new("Vale", "ADC", "Confirmed"),
                    new("Orin", "Jungle", "Confirmed")
                ])
            ],
            [
                new("Verify roster ownership", "Transfer captain rights to the active owner before matchday if the original captain cannot play.", "Ownership stays with the team, not the user account.", "Before 24 May", "warn"),
                new("Accept Discord invite", "The login flow can later enforce Discord presence for tournament-only coordination rooms.", "Mocked now, real webhook later.", "Any time", "neutral"),
                new("Acknowledge fair-play policy", "Every captain and player will eventually sign the same tournament policy bundle.", "Copy ready for backend wiring.", "Still needed", "positive")
            ],
            [
                new("Review waitlist", "A final call is needed on whether the waitlist converts automatically once a slot opens.", "This is a backend workflow placeholder for staff tooling.", "Open", "warn"),
                new("Approve format lock", "Swiss rounds and cut rules should become immutable after public announcement.", "Good candidate for an admin-only settings screen.", "Ready for product decision", "neutral"),
                new("Seed observer accounts", "Broadcast and referee observers need a lightweight access profile in the future identity model.", "Will likely depend on tournament-scoped roles.", "Investigate", "positive")
            ],
            [
                new("Discover", "Landing pages and discovery flow", "Public tournament cards are ready for the frontend and backed by mock data.", "Done", "positive"),
                new("Player space", "Invites, checklist, roster view", "The frontend already separates authenticated team functionality from public content.", "In progress", "warn"),
                new("Administration", "Desk, seeding, schedule operations", "Only mock UIs exist for now; the backend stays intentionally absent.", "Next", "neutral")
            ]),
        new(
            new TournamentSummary(
                "valorant-campus-clash-2026",
                "Campus Clash",
                "Autumn 2026",
                "VALORANT",
                "16 Team GSL Groups",
                "LAN Finals in Kempten",
                "Registration opens 01 July",
                "Planned",
                "neutral",
                "A second ruleset showing the platform can branch away from League without changing the frontend shell.",
                0,
                16),
            "Same tournament shell, different game logic later.",
            "This event exists to prove the app is not hardcoded to a single title. The backend can evolve game-specific modules later.",
            "Client onboarding and team import start in July.",
            [
                new("Projected slots", "16", "Scoped to a tighter LAN footprint."),
                new("Partner schools", "6", "Mocked from outreach discussions."),
                new("Volunteer staff", "9", "Enough for desks, refs, and broadcast support."),
                new("Ruleset maturity", "Concept", "Still in planning.")
            ],
            [
                new("Concept", "Venue sizing started", "The current mockup assumes a one-stage LAN final with online groups.", "This week", "neutral")
            ],
            [
                new("Planning", "TBD", "Venue", "Qualified Team 1", "Qualified Team 2", "Open", "Open", "Blocked", "warn", "No lineups or seeds should surface until the backend exists.")
            ],
            [
                new("Placeholder Squad", "TBD", "No seed yet", "Reserved to keep team cards reusable across games.", [
                    new("Player One", "Captain", "Planned"),
                    new("Player Two", "Flex", "Planned")
                ])
            ],
            [
                new("Create shared team", "The same team object can join multiple tournaments once backend support lands.", "Explicitly mentioned in the README vision.", "Future", "neutral")
            ],
            [
                new("Define LAN staffing", "Admin tooling will eventually need staff-only task queues and incident tracking.", "Current panel is only a mock.", "Later", "neutral")
            ],
            [
                new("Planning", "Concept discovery", "Useful as a second tournament selector entry in the sidebar.", "Open", "neutral")
            ]),
        new(
            new TournamentSummary(
                "fc-showdown-2026",
                "FC Showdown",
                "Winter 2026",
                "EA Sports FC",
                "Solo bracket via single-player teams",
                "Remote",
                "Registration planned for October",
                "Exploring",
                "warn",
                "Demonstrates how single-player tournaments can be modeled as teams of one.",
                0,
                64),
            "Single-player tournaments stay compatible by wrapping players in teams of one.",
            "This follows the README direction directly: teams stay the core participant model even when the competition is nominally solo.",
            "Rules alignment is still open.",
            [
                new("Bracket size", "64", "Solo players represented by solo teams."),
                new("Automation", "Low", "Needs a game-specific result import plan."),
                new("Identity need", "High", "Player invites matter even more for solo ladders."),
                new("Readiness", "Discovery", "Useful placeholder for product scope.")
            ],
            [
                new("Idea", "Solo team modeling confirmed", "The frontend foundation already treats teams as reusable containers.", "Today", "positive")
            ],
            [
                new("Discovery", "TBD", "Remote", "Solo Team A", "Solo Team B", "Unseeded", "Unseeded", "Waiting", "neutral", "Pure placeholder for now.")
            ],
            [
                new("Solo Slot", "N/A", "No standing", "An example one-player roster.", [
                    new("Single Player", "Owner", "Planned")
                ])
            ],
            [
                new("Invite alt account", "Solo formats still benefit from identity-linked participant access.", "A simple frontend pattern should carry over.", "Future", "neutral")
            ],
            [
                new("Define anti-smurf checks", "Any serious FC format will need a clearer verification story.", "Out of scope for the first backend.", "Open", "warn")
            ],
            [
                new("Discovery", "Future scope item", "Included to make selector behavior obvious.", "Backlog", "neutral")
            ])
    ];

    public IReadOnlyList<TournamentSummary> GetAll()
        => _tournaments.Select(tournament => tournament.Summary).ToList();

    public TournamentDetail GetFeatured()
        => _tournaments[0];

    public TournamentDetail? Find(string slug)
        => _tournaments.FirstOrDefault(tournament =>
            string.Equals(tournament.Summary.Slug, slug, StringComparison.OrdinalIgnoreCase));

    public TournamentSummary? FindSummary(string slug)
        => GetAll().FirstOrDefault(tournament =>
            string.Equals(tournament.Slug, slug, StringComparison.OrdinalIgnoreCase));
}
