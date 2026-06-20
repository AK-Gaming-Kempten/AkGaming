import { Event, Post, type PostContentComponent } from "./types";
import { postModules } from "./postModules";

type FrontMatter = {
    type?: "post" | "event";
    id: string;
    title: string;
    shortDescription: string;
    startDate?: string;
    endDate?: string;
    location?: string;
    locationUrl?: string;
};

export async function loadPosts(): Promise<(Post | Event)[]> {
    const items: (Post | Event)[] = [];

    for (const module of postModules) {
        const fmData = (module as { frontmatter?: FrontMatter }).frontmatter;

        if (fmData === undefined) {
            throw new Error("Missing front matter in post module.");
        }

        switch (fmData.type) {
            case "event": {
                items.push(
                    new Event({
                        id: fmData.id,
                        title: fmData.title,
                        shortDescription: fmData.shortDescription,
                        Content: (module as { default: PostContentComponent }).default,
                        startDate: fmData.startDate!,
                        endDate: fmData.endDate,
                        location: fmData.location!,
                        locationUrl: fmData.locationUrl,
                    })
                );
                break;
            }
            default: {
                items.push(
                    new Post({
                        id: fmData.id,
                        title: fmData.title,
                        shortDescription: fmData.shortDescription,
                        Content: (module as { default: PostContentComponent }).default,
                    })
                );
            }
        }
    }
    return items;
}
