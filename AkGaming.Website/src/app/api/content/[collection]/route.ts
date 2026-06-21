import { promises as fs } from "node:fs";
import type { Dirent } from "node:fs";
import path from "node:path";
import { NextRequest, NextResponse } from "next/server";
import YAML from "yaml";
import { listHighlights } from "../../../../content/highlightStore";
import { listPublishedPosts } from "../../../../content/postStore";

const dataDirectory = path.join(process.cwd(), "src", "data");
const mediaDirectory = path.join(process.cwd(), "public", "media");
const supportedImageExtensions = new Set([".jpg", ".jpeg", ".png", ".webp", ".avif", ".gif"]);

type ContentRouteContext = {
    params: Promise<{ collection: string }>;
};

export async function GET(request: NextRequest, context: ContentRouteContext) {
    const { collection } = await context.params;

    switch (collection) {
        case "games":
            return NextResponse.json(await readYamlFile(path.join(dataDirectory, "games.yaml")));
        case "teams":
            return NextResponse.json(await readYamlDirectory(path.join(dataDirectory, "teams")));
        case "highlights":
            return NextResponse.json(await listHighlights());
        case "posts":
            return NextResponse.json(await listPublishedPosts());
        case "images":
            return NextResponse.json(await readImages(request));
        default:
            return NextResponse.json({ message: "Unknown content collection." }, { status: 404 });
    }
}

async function readYamlFile(filePath: string): Promise<unknown> {
    const source = await fs.readFile(filePath, "utf8");
    return YAML.parse(source);
}

async function readYamlDirectory(directoryPath: string): Promise<unknown[]> {
    const entries = await fs.readdir(directoryPath, { withFileTypes: true });
    const yamlFiles = entries
        .filter(entry => entry.isFile() && [".yaml", ".yml"].includes(path.extname(entry.name).toLowerCase()))
        .map(entry => path.join(directoryPath, entry.name));

    return Promise.all(yamlFiles.map(readYamlFile));
}

async function readImages(request: NextRequest): Promise<string[]> {
    const folder = request.nextUrl.searchParams.get("folder")?.trim() ?? "";
    const requestedDirectory = path.resolve(mediaDirectory, folder);
    const allowedPrefix = `${mediaDirectory}${path.sep}`;

    if (!requestedDirectory.startsWith(allowedPrefix))
        return [];

    let entries: Dirent<string>[];
    try {
        entries = await fs.readdir(requestedDirectory, { encoding: "utf8", withFileTypes: true });
    }
    catch (error) {
        if (isMissingDirectory(error))
            return [];

        throw error;
    }

    return entries
        .filter(entry => entry.isFile() && supportedImageExtensions.has(path.extname(entry.name).toLowerCase()))
        .map(entry => `/media/${folder}/${entry.name}`)
        .sort((left, right) => left.localeCompare(right));
}

function isMissingDirectory(error: unknown): boolean {
    return typeof error === "object"
           && error !== null
           && "code" in error
           && error.code === "ENOENT";
}
