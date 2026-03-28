import YAML from "yaml";
import type { EsportsTeam } from "./types.ts";

export async function loadTeams(): Promise<EsportsTeam[]> {
    const jsonModules = import.meta.glob<EsportsTeam>("./teams/*.json", {
        eager: true,
        import: "default",
    });

    const yamlModules = import.meta.glob<string>("./teams/*.{yaml,yml}", {
        eager: true,
        query: "?raw",
        import: "default",
    });

    const teams: EsportsTeam[] = [];

    // Parse JSON teams
    for (const key in jsonModules) {
        teams.push(jsonModules[key]);
    }

    // Parse YAML teams
    for (const key in yamlModules) {
        const raw = yamlModules[key];
        const parsed = YAML.parse(raw) as EsportsTeam;
        teams.push(parsed);
    }

    return teams;
}

export async function loadTeamsByGame(game: string): Promise<EsportsTeam[]> {
    const all = await loadTeams();
    return all.filter((t) => t.game.toLowerCase() === game.toLowerCase());
}
