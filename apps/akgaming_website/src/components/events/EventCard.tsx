import "./EventCard.css";
import { Link } from "react-router-dom";
import { formatDateRange } from "../../utils/formatDateRange";
import type { Event } from "../../data/types";

interface EventCardProps {
    event: Event;
}

export default function EventCard({ event }: EventCardProps) {
    const formattedDate = formatDateRange(event.startDate, event.endDate);

    const locationElement = event.location.startsWith("http") ? (
        <a href={event.location} target="_blank" rel="noopener noreferrer">
            {event.location}
        </a>
    ) : (
        <span>{event.location}</span>
    );

    return (
        <Link to={`/events/${event.id}`} className="event-card">
            <div className="event-card-header">
                <h3 className="event-name">{event.title}</h3>
                <p className="event-date">{formattedDate}</p>
            </div>

            <p className="event-description">{event.shortDescription}</p>

            <div className="event-location">
                <span>📍 {locationElement}</span>
            </div>
        </Link>
    );
}
