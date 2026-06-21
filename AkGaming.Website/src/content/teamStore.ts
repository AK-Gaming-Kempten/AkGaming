import "server-only";

import { promises as fs } from "node:fs";
import path from "node:path";
import YAML from "yaml";
import { listManagedLeagues } from "./esportsCatalogStore";

export type ManagedEsportsPlayer = {
    name: string;
    role: string;
    picture: string;
};

export type ManagedEsportsTeam = {
    id: string;
    game: string;
    name: string;
    logo: string;
    leagueId: string;
    division: string;
    players: ManagedEsportsPlayer[];
};

const validTeamId = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;

export async function listManagedTeams(): Promise<ManagedEsportsTeam[]> {
    const directory = getTeamsDirectory();
    let entries: string[];
    try {
        entries = await fs.readdir(directory);
    }
    catch (error) {
        if (isMissingPath(error)) return [];
        throw error;
    }

    const teams = await Promise.all(entries
        .filter(fileName => [".yaml", ".yml"].includes(path.extname(fileName).toLowerCase()))
        .map(fileName => readTeam(path.join(directory, fileName))));
    return teams.sort((left, right) => left.name.localeCompare(right.name));
}

export async function saveManagedTeam(team: ManagedEsportsTeam, previousId?: string): Promise<ManagedEsportsTeam> {
    validateTeam(team);
    if (previousId !== undefined && previousId !== team.id)
        validateTeamId(previousId);

    const directory = getTeamsDirectory();
    await fs.mkdir(directory, { recursive: true });
    const source = YAML.stringify({
        game: team.game,
        name: team.name,
        logo: team.logo,
        leagueId: team.leagueId,
        division: team.division,
        players: team.players,
    });
    await writeFileAtomically(path.join(directory, `${team.id}.yaml`), source);

    if (previousId !== undefined && previousId !== team.id)
        await removeTeamFiles(directory, previousId);

    return team;
}

export async function listPublicTeams() {
    const [teams, leagues] = await Promise.all([listManagedTeams(), listManagedLeagues()]);
    return teams.map(team => {
        const league = leagues.find(candidate => candidate.id === team.leagueId);
        if (league === undefined)
            throw new Error(`Team '${team.name}' references unknown league '${team.leagueId}'.`);

        return {
            ...team,
            league: { name: league.name, logo: league.logo, division: team.division },
        };
    });
}

export async function deleteManagedTeam(id: string): Promise<void> {
    validateTeamId(id);
    await removeTeamFiles(getTeamsDirectory(), id);
}

async function readTeam(filePath: string): Promise<ManagedEsportsTeam> {
    const source = await fs.readFile(filePath, "utf8");
    const parsed = YAML.parse(source) as Omit<ManagedEsportsTeam, "id"> & { league?: { name?: string; division?: string } };
    const id = path.basename(filePath, path.extname(filePath));
    const team: ManagedEsportsTeam = {
        id,
        game: parsed.game ?? "",
        name: parsed.name ?? "",
        logo: parsed.logo ?? "",
        leagueId: parsed.leagueId ?? toLeagueId(parsed.league?.name ?? ""),
        division: parsed.division ?? parsed.league?.division ?? "",
        players: (parsed.players ?? []).map(player => ({
            name: player.name ?? "",
            role: player.role ?? "",
            picture: player.picture ?? "",
        })),
    };
    validateTeam(team);
    return team;
}

function validateTeam(team: ManagedEsportsTeam): void {
    validateTeamId(team.id);
    if (!team.game.trim()) throw new Error("A game ID is required.");
    if (!team.name.trim()) throw new Error("A team name is required.");
    if (!team.logo.trim()) throw new Error("A team logo URL is required.");
    if (!team.leagueId.trim()) throw new Error("A league ID is required.");
    if (!team.division.trim()) throw new Error("A league division is required.");
    for (const player of team.players) {
        if (!player.name.trim()) throw new Error("Each player needs a name.");
        if (!player.picture.trim()) throw new Error("Each player needs an image URL.");
    }
}

function validateTeamId(id: string): void {
    if (!validTeamId.test(id))
        throw new Error("Team IDs must use lowercase letters, numbers, and hyphens only.");
}

function toLeagueId(name: string): string {
    return name.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-+|-+$/g, "");
}

function getContentRoot(): string {
    const configuredRoot = process.env.AKG_WEBSITE_CONTENT_ROOT;
    return configuredRoot ? path.resolve(configuredRoot) : path.join(process.cwd(), "src", "data");
}

function getTeamsDirectory(): string {
    return path.join(getContentRoot(), "teams");
}

async function removeTeamFiles(directory: string, id: string): Promise<void> {
    let removed = false;
    for (const extension of [".yaml", ".yml"]) {
        try {
            await fs.unlink(path.join(directory, `${id}${extension}`));
            removed = true;
        }
        catch (error) {
            if (!isMissingPath(error)) throw error;
        }
    }
    if (!removed)
        throw new Error(`Team '${id}' does not exist.`);
}

async function writeFileAtomically(filePath: string, source: string): Promise<void> {
    const temporaryPath = `${filePath}.${process.pid}.${Date.now()}.tmp`;
    await fs.writeFile(temporaryPath, source, "utf8");
    await fs.rename(temporaryPath, filePath);
}

function isMissingPath(error: unknown): boolean {
    return typeof error === "object" && error !== null && "code" in error && error.code === "ENOENT";
}
