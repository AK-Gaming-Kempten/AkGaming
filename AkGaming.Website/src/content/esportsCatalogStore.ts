import "server-only";

import { promises as fs } from "node:fs";
import path from "node:path";
import YAML from "yaml";

export type ManagedEsportsGame = {
    id: string;
    displayName: string;
    logo: string;
};

export type ManagedEsportsLeague = {
    id: string;
    name: string;
    logo: string;
};

const validId = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;

export async function listManagedGames(): Promise<ManagedEsportsGame[]> {
    return readCatalog<ManagedEsportsGame>("games.yaml", validateGame);
}

export async function saveManagedGames(games: ManagedEsportsGame[]): Promise<ManagedEsportsGame[]> {
    const normalized = games.map(validateGame);
    await writeCatalog("games.yaml", normalized);
    return normalized;
}

export async function listManagedLeagues(): Promise<ManagedEsportsLeague[]> {
    return readCatalog<ManagedEsportsLeague>("leagues.yaml", validateLeague);
}

export async function saveManagedLeagues(leagues: ManagedEsportsLeague[]): Promise<ManagedEsportsLeague[]> {
    const normalized = leagues.map(validateLeague);
    await writeCatalog("leagues.yaml", normalized);
    return normalized;
}

function validateGame(value: ManagedEsportsGame): ManagedEsportsGame {
    validateId(value.id, "Game");
    if (!value.displayName?.trim()) throw new Error("Each game needs a display name.");
    if (!value.logo?.trim()) throw new Error("Each game needs a logo URL.");
    return { id: value.id, displayName: value.displayName.trim(), logo: value.logo.trim() };
}

function validateLeague(value: ManagedEsportsLeague): ManagedEsportsLeague {
    validateId(value.id, "League");
    if (!value.name?.trim()) throw new Error("Each league needs a name.");
    if (!value.logo?.trim()) throw new Error("Each league needs a logo URL.");
    return { id: value.id, name: value.name.trim(), logo: value.logo.trim() };
}

function validateId(id: string, kind: string): void {
    if (!validId.test(id)) throw new Error(`${kind} IDs must use lowercase letters, numbers, and hyphens only.`);
}

async function readCatalog<T>(fileName: string, validate: (value: T) => T): Promise<T[]> {
    try {
        const source = await fs.readFile(path.join(getContentRoot(), fileName), "utf8");
        const values = YAML.parse(source) as T[];
        return Array.isArray(values) ? values.map(validate) : [];
    }
    catch (error) {
        if (isMissingPath(error)) return [];
        throw error;
    }
}

async function writeCatalog<T>(fileName: string, values: T[]): Promise<void> {
    const root = getContentRoot();
    await fs.mkdir(root, { recursive: true });
    await fs.writeFile(path.join(root, fileName), YAML.stringify(values), "utf8");
}

function getContentRoot(): string {
    const configuredRoot = process.env.AKG_WEBSITE_CONTENT_ROOT;
    return configuredRoot ? path.resolve(configuredRoot) : path.join(process.cwd(), "src", "data");
}

function isMissingPath(error: unknown): boolean {
    return typeof error === "object" && error !== null && "code" in error && error.code === "ENOENT";
}
