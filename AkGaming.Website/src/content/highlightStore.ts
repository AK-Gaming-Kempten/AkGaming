import "server-only";

import { promises as fs } from "node:fs";
import path from "node:path";
import YAML from "yaml";

export type ContentHighlight = {
    postId: string;
    mediaSrc: string;
    mediaType: "image" | "video";
    title?: string;
    description?: string;
};

export async function listHighlights(): Promise<ContentHighlight[]> {
    try {
        const source = await fs.readFile(getHighlightsPath(), "utf8");
        const highlights = YAML.parse(source) as unknown;
        return Array.isArray(highlights) ? highlights.map(validateHighlight) : [];
    }
    catch (error) {
        if (isMissingPath(error))
            return [];

        throw error;
    }
}

export async function saveHighlights(highlights: ContentHighlight[]): Promise<ContentHighlight[]> {
    const normalized = highlights.map(validateHighlight);
    await fs.mkdir(getContentRoot(), { recursive: true });
    await fs.writeFile(getHighlightsPath(), YAML.stringify(normalized), "utf8");
    return normalized;
}

function validateHighlight(value: unknown): ContentHighlight {
    if (typeof value !== "object" || value === null)
        throw new Error("Each highlight must be an object.");

    const highlight = value as Partial<ContentHighlight>;
    const postId = highlight.postId?.trim() ?? "";
    const mediaSrc = highlight.mediaSrc?.trim() ?? "";
    if (!postId)
        throw new Error("Each highlight must reference a post.");
    if (!mediaSrc)
        throw new Error("Each highlight needs a media URL.");
    if (highlight.mediaType !== "image" && highlight.mediaType !== "video")
        throw new Error("Highlight media type must be image or video.");

    return {
        postId,
        mediaSrc,
        mediaType: highlight.mediaType,
        ...(highlight.title?.trim() ? { title: highlight.title.trim() } : {}),
        ...(highlight.description?.trim() ? { description: highlight.description.trim() } : {}),
    };
}

function getContentRoot(): string {
    const configuredRoot = process.env.AKG_WEBSITE_CONTENT_ROOT;
    return configuredRoot ? path.resolve(configuredRoot) : path.join(process.cwd(), "src", "data");
}

function getHighlightsPath(): string {
    return path.join(getContentRoot(), "highlights.yaml");
}

function isMissingPath(error: unknown): boolean {
    return typeof error === "object" && error !== null && "code" in error && error.code === "ENOENT";
}
