import type { Event } from "../../data/types";
import EventCard from "./EventCard";
import "./UpcomingEventGrid.css";

export default function UpcomingEventGrid({ events }: { events: Event[] }) {
    if (events.length === 0) {
        return <p className="upcoming-events-empty">Aktuell sind keine Termine angekündigt.</p>;
    }

    return (
        <div className="upcoming-events-grid">
            {events.map(event => <EventCard key={event.id} event={event} />)}
        </div>
    );
}
