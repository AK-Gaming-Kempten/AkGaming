import type { ComponentType } from "react";

export type PostContentProps = {
    components?: Record<string, unknown>;
};

export type PostContentComponent = ComponentType<PostContentProps>;

export class Post {
    id: string;
    title: string;
    shortDescription: string;
    Content: PostContentComponent;

    constructor(params: { id: string; title: string; shortDescription: string; Content: PostContentComponent }) {
        this.id = params.id;
        this.title = params.title;
        this.shortDescription = params.shortDescription;
        this.Content = params.Content;
    }
}

export class Event extends Post {
    startDate: string;
    endDate?: string;
    location: string;
    locationUrl?: string;

    constructor(params: {
        id: string;
        title: string;
        shortDescription: string;
        Content: PostContentComponent;
        startDate: string;
        endDate?: string;
        location: string;
        locationUrl?: string;
    }) {
        super(params);
        this.startDate = params.startDate;
        this.endDate = params.endDate;
        this.location = params.location;
        this.locationUrl = params.locationUrl;
    }
}

export interface Highlight {
    postId: string;
    mediaSrc: string;
    mediaType: "image" | "video";
    title?: string;
    description?: string;
}

export interface EsportsGame {
    id: string;
    displayName: string;
    logo: string;
}

export interface EsportsPlayer {
    name: string;
    role: string;
    picture: string;
}

export interface EsportsLeague {
    name: string;
    logo: string;
    division: string;
}

export interface EsportsTeam {
    game: string;
    name: string;
    logo: string;
    league: EsportsLeague;
    players: EsportsPlayer[];
}
