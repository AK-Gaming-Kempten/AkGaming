import YAML from "yaml";
import type { EsportsGame } from "./types";

export function loadGames(): EsportsGame[] {
    const module = import.meta.glob<string>("./games.yaml", {
        eager: true,
        as: "raw",
    });

    const raw = Object.values(module)[0];
    return YAML.parse(raw as string) as EsportsGame[];
}
