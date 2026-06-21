import { Event, Post } from "./types";

type ContentPost = {
    type: "post" | "event";
    id: string;
    title: string;
    shortDescription: string;
    startDate?: string;
    endDate?: string;
    location?: string;
    locationUrl?: string;
};

export async function loadPosts(): Promise<(Post | Event)[]> {
    const response = await fetch("/api/content/posts");
    if (!response.ok)
        throw new Error("Unable to load website posts.");

    const contentPosts = await response.json() as ContentPost[];
    return contentPosts.map(post => {
        switch (post.type) {
            case "event": {
                return new Event({
                    id: post.id,
                    title: post.title,
                    shortDescription: post.shortDescription,
                    startDate: post.startDate ?? "",
                    endDate: post.endDate,
                    location: post.location ?? "",
                    locationUrl: post.locationUrl,
                });
            }
            default: {
                return new Post({
                    id: post.id,
                    title: post.title,
                    shortDescription: post.shortDescription,
                });
            }
        }
    });
}
