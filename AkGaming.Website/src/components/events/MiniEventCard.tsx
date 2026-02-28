import "./EventCard.css";
import { Link } from "react-router-dom";
import { formatDateRange } from "../../utils/formatDateRange";
import type { Event } from "../../data/types";

interface MiniEventCardProps {
    event: Event;
}

export default function MiniEventCard({ event }: MiniEventCardProps) {
    const formattedDate = formatDateRange(event.startDate, event.endDate);

    return (
        <Link to={`/events/${event.id}`} className="event-card">
            <h5 className="event-name">{event.title}</h5>
            <p className="event-date">{formattedDate}</p>
        </Link>
    );
}
