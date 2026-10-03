import "./EventCard.css";
import Link from "next/link";
import { formatDateRange } from "../../utils/formatDateRange";
import type { Event } from "../../data/types";
import type { FC } from "react";

type EventCardProps = {
    event: Event;
};

const EventCard: FC<EventCardProps> = ({ event }) => {
    const formattedDate = formatDateRange(event.startDate, event.endDate);

    return (
        <Link href={`/events/${event.id}`} className="event-card">
            <div className="event-card-header">
                <h3 className="event-name">{event.title}</h3>
                <p className="event-date">{formattedDate}</p>
            </div>

            <p className="event-description">{event.shortDescription}</p>

            <div className="event-location">
                <span>📍 {event.location}</span>
            </div>
        </Link>
    );
};

export default EventCard;
