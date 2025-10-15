// src/data/loadPosts.ts
import fm from "front-matter";
import { marked } from "marked";
import { Post, Event } from "./types";

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
    const mdModules = import.meta.glob<string>("./posts/*.md", {
        eager: true,
        query: "?raw", // ✅ Vite 7 syntax
        import: "default",
    });

    const items: (Post | Event)[] = [];

    for (const key in mdModules) {
        const raw = mdModules[key]; // plain string
        const { attributes, body } = fm<FrontMatter>(raw); // ✅ front-matter parses directly
        const html = await marked.parse(body);

        const fmData = attributes;

        switch (fmData.type){
            case "event": {
                items.push(
                    new Event({
                        id: fmData.id,
                        title: fmData.title,
                        shortDescription: fmData.shortDescription,
                        text: html,
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
                        text: html,
                    })
                );
            }
        }
    }
    return items;
}