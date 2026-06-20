import type { EsportsGame } from "./types";

export async function loadGames(): Promise<EsportsGame[]> {
    const response = await fetch("/api/content/games");
    if (!response.ok)
        throw new Error("Unable to load esports games.");

    return response.json() as Promise<EsportsGame[]>;
}
