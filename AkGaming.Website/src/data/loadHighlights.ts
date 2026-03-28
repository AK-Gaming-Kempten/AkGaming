import YAML from "yaml";
import type { Highlight } from "./types";
import { loadPosts } from "./loadPosts";

export async function loadHighlights(): Promise<Highlight[]> {
    // Load raw YAML file
    const module = import.meta.glob<string>("./highlights.yaml", {
        eager: true,
        query: "?raw",
        import: "default",
    });

    const raw = Object.values(module)[0];
    const highlights = YAML.parse(raw as string) as Highlight[];

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
