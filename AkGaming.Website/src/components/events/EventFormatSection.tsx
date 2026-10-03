import type { Event } from "../../data/types";
import UpcomingEventGrid from "./UpcomingEventGrid";
import "./EventFormatSection.css";

interface EventFormat {
    id: string;
    title: string;
    content: string;
    imageSrc: string;
    imageAlt: string;
}

export default function EventFormatSection({ section, events }: { section: EventFormat; events: Event[] }) {
    return (
        <section className="event-format-section" aria-labelledby={`event-format-${section.id}`}>
            <div className="event-format-content ak-content">
                <div className="event-format-copy">
                    <h2 id={`event-format-${section.id}`}>{section.title}</h2>
                    <p>{section.content}</p>
                </div>
                <img className="event-format-image" src={section.imageSrc} alt={section.imageAlt} loading="lazy" />
            </div>
            <div className="event-format-dates ak-content">
                <h3>Kommende Termine</h3>
                <UpcomingEventGrid events={events} />
            </div>
        </section>
    );
}
