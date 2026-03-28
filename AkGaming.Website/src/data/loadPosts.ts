import { Event, Post, type PostContentComponent } from "./types";

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

type PostModule = {
    default: PostContentComponent;
    frontmatter?: FrontMatter;
};

export async function loadPosts(): Promise<(Post | Event)[]> {
    const contentModules = import.meta.glob<PostModule>("./posts/*.{md,mdx}", {
        eager: true,
    });

    const items: (Post | Event)[] = [];

    for (const key in contentModules) {
        const module = contentModules[key];
        const fmData = module.frontmatter;

        if (fmData === undefined) {
            throw new Error(`Missing front matter in post module: ${key}`);
        }

        switch (fmData.type) {
            case "event": {
                items.push(
                    new Event({
                        id: fmData.id,
                        title: fmData.title,
                        shortDescription: fmData.shortDescription,
                        Content: module.default,
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
                        Content: module.default,
                    })
                );
            }
        }
    }
    return items;
}
