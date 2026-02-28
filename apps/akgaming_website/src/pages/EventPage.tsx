import { useParams } from "react-router-dom";
import { useEffect, useState } from "react";
import { loadPosts } from "../data/loadPosts";
import { Event } from "../data/types";
import { formatDateRange } from "../utils/formatDateRange";
import "./PostPage.css";

export default function EventPage() {
    const { postId } = useParams();
    const [event, setEvent] = useState<Event | null>(null);

    useEffect(() => {
        loadPosts().then((data) => {
            const found = data.find((p) => p.id === postId);
            setEvent(found as Event ?? null);
        });
    }, [postId]);

    if (!event) return <p>Loading...</p>;

    const locationElement = event.locationUrl !== undefined ? (
        <a href={event.locationUrl} target="_blank" rel="noopener noreferrer">
            {event.location}
        </a>
    ) : (
        <span>{event.location}</span>
    );

    return (
        <main className="post-page">
            <h1>{event.title}</h1>
            <p className="post-short">{event.shortDescription}</p>
            <div className="post-content">

                <p className="post-meta">
                    📅 {formatDateRange(event.startDate, event.endDate)}<br />
                    <span>📍 {locationElement}</span>
                </p>
                <div className="post-text" dangerouslySetInnerHTML={{ __html: event.text }} />
            </div>
        </main>
    );
}