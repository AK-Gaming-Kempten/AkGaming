import type { EsportsTeam } from "./types.ts";

export async function loadTeams(): Promise<EsportsTeam[]> {
    const response = await fetch("/api/content/teams");
    if (!response.ok)
        throw new Error("Unable to load esports teams.");

    return response.json() as Promise<EsportsTeam[]>;
}

export async function loadTeamsByGame(game: string): Promise<EsportsTeam[]> {
    const all = await loadTeams();
    return all.filter((t) => t.game.toLowerCase() === game.toLowerCase());
}
