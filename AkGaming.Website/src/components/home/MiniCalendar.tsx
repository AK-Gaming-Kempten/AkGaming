import "./MiniCalendar.css";
import { useEffect, useState } from "react";
import {loadPosts} from "../../data/loadPosts";
import { Event } from "../../data/types";
import MiniEventCard from "../events/MiniEventCard.tsx";
import { getVisibleUpcomingEvents } from "../../utils/eventDates";

export default function MiniCalendar() {
    const [events, setEvents] = useState<Event[]>([]);

    useEffect(() => {
        loadPosts().then((data) => {
            const found = data.filter((p) => p instanceof Event);
            setEvents(found as Event[] ?? null);
        });
    }, []);

    const visibleUpcomingEvents = getVisibleUpcomingEvents(events);

    return (
        <div className="mini-calendar">
            <h3 className="calendar-title">Kommende Events</h3>

            {visibleUpcomingEvents.length === 0 ? (
                <p className="calendar-empty">Keine bevorstehenden Events</p>
            ) : (
                <div className="calendar-list">
                    {visibleUpcomingEvents.map((e) => (
                        <MiniEventCard key={e.id} event={e} />
                    ))}
                </div>
            )}
        </div>
    );
}
