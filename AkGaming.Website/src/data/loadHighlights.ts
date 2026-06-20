import type { Highlight } from "./types";
import { loadPosts } from "./loadPosts";

export async function loadHighlights(): Promise<Highlight[]> {
    const response = await fetch("/api/content/highlights");
    if (!response.ok)
        throw new Error("Unable to load homepage highlights.");

    const highlights = await response.json() as Highlight[];

    // Load posts so we can enrich missing data
    const posts = await loadPosts();

    return highlights.map((h) => {
        const post = posts.find((p) => p.id === h.postId);
        return {
            ...h,
            title: h.title ?? post?.title ?? "Untitled",
            description: h.description ?? post?.shortDescription ?? "",
        };
    });
}
