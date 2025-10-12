import "./EventCard.css";
import { Link } from "react-router-dom";
import { formatDateRange } from "../../utils/formatDateRange";

interface EventCardProps {
    id: string;
    name: string;
    dateStart: string;
    dateEnd?: string;
    description: string;
    locationName: string;
    locationUrl?: string;
}

export default function EventCard({
                                      id,
                                      name,
                                      dateStart,
                                      dateEnd,
                                      description,
                                      locationName,
                                      locationUrl,
                                  }: EventCardProps) {
    const formattedDate = formatDateRange(dateStart, dateEnd);

    const locationElement = locationUrl ? (
        <a href={locationUrl} target="_blank" rel="noopener noreferrer">
            {locationName}
        </a>
    ) : (
        <span>{locationName}</span>
    );

    return (
        <Link to={`/events/${id}`} className="event-card">
            <div className="event-card-header">
                <h3 className="event-name">{name}</h3>
                <p className="event-date">{formattedDate}</p>
            </div>

            <p className="event-description">{description}</p>

            <div className="event-location">
                <span>📍 {locationElement}</span>
            </div>
        </Link>
    );
}
