import "./EventCard.css";
import { Link } from "react-router-dom";
import { formatDateRange } from "../../utils/formatDateRange";
import type { Event } from "../../data/types";
import type { FC } from "react";

type MiniEventCardProps = {
    event: Event;
};

const MiniEventCard: FC<MiniEventCardProps> = ({ event }) => {
    const formattedDate = formatDateRange(event.startDate, event.endDate);

    return (
        <Link to={`/events/${event.id}`} className="event-card">
            <h5 className="event-name">{event.title}</h5>
            <p className="event-date">{formattedDate}</p>
        </Link>
    );
};

export default MiniEventCard;
