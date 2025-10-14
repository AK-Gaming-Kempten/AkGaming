export class Post {
    id: string;
    title: string;
    shortDescription: string;
    text: string;

    constructor(params: { id: string; title: string; shortDescription: string; text: string }) {
        this.id = params.id;
        this.title = params.title;
        this.shortDescription = params.shortDescription;
        this.text = params.text;
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
        text: string;
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

