"use client";

import UpcomingEventGrid from "../components/events/UpcomingEventGrid";
import { EventType } from "../data/eventTypes";
import "./Events.css";
import EventFormatSection from "../components/events/EventFormatSection";
import { loadPosts } from "../data/loadPosts";
import { Event } from "../data/types";
import { useEffect, useState } from "react";
import { getEventBounds, getVisibleUpcomingEvents } from "../utils/eventDates";

export default function Events() {
    const infoSections = [
        {
            id: EventType.Gamenight,
            title: "Gamenight",
            content:
                "Auf der Game Night kommen Menschen zusammen, um einen Abend voller Spielspaß zu erleben und die Gemeinschaft zu stärken. Dafür bietet die Game Night ein vielseitiges Programm: Besucher können ihr eigenes Gaming-Setup mitbringen und im LAN-Netzwerk gemeinsam spielen. Auch ohne eigene Hardware gibt es jede Menge zu erleben: Wir stellen einige moderne sowie Retro-Konsolen, eine riesige Auswahl an Brettspielen, einen VR-Raum, moderierte Turniere mit Sachpreisen, Projektpräsentationen, Karaoke und Special Events – da ist auf jeden Fall für Alle etwas dabei!",
            imageSrc: "/media/eventInfoGallery/gamenight/gamenight-community.webp",
            imageAlt: "Community bei der AK Gaming Game Night",
        },
        {
            id: EventType.GameJam,
            title: "Game Jam",
            content:
                "Unser Game Jam ist eine Veranstaltung für jeden, der schon immer mal ein Computerspiel entwickeln wollte, bereits Profi in der Disziplin ist oder sich dafür interessiert, was dabei rauskommt, wenn jemand in wenigen Tagen so etwas auf die Beine stellt. Interessierte treffen sich hier um in einem festen Zeitrahmen und einer thematischen Vorgabe ein Spiel zu entwickeln. Die fertigen Spiele werden im Anschluss bewertet und neben der einmaligen Erfahrung winken für tolle Ideen und Umsetzungen auch Preisgelder. Egal ob 3D Artist, Game Designer oder Programmierer, als Teil eines kleinen Teams oder ganz alleine kann jeder mitmachen.",
            imageSrc: "/media/eventInfoGallery/gamejam/gamejam-1-6-collage.webp",
            imageAlt: "Collage aus 39 Spiele-Thumbnails der ersten sechs AK Gaming Game Jams",
        },
        {
            id: EventType.BoardGames,
            title: "Brettspielabend",
            content:
                "In Zusammenarbeit mit der Heldenschmiede stellen wir regelmäßig eine riesige Auswahl an Brettspielen bei unserem Brettspielabend zur Verfügung. Hier kann jeder vorbei kommen, Leute kennenlernen, neue Spiele ausprobieren oder die 1000ste Runde seines Lieblingsspiels starten.",
            imageSrc: "/media/eventInfoGallery/boardgames/brettspielabend-2022.webp",
            imageAlt: "Brettspielabend: Eine Spielfigur wird auf dem Spielbrett bewegt",
        },
    ];

    const [events, setEvents] = useState<Event[]>([]);

    useEffect(() => {
        loadPosts().then((data) => {
            const found = data.filter((p) => p instanceof Event);
            setEvents(found as Event[] ?? []);
        });
    }, []);

    const nowMs = Date.now();
    const visibleUpcomingEvents = getVisibleUpcomingEvents(events, nowMs).filter(event => (getEventBounds(event)?.endMs ?? 0) >= nowMs);
    const otherEvents = visibleUpcomingEvents.filter(event => event.eventType === EventType.Other);

    return (
        <div className="events-page">
            <section className="events-hero">
                <p className="events-eyebrow">AK Gaming e.V.</p>
                <h1>Events</h1>
                <p className="events-hero-copy">
                    Lokale und digitale Formate für Community, Wettbewerb und Austausch.
                    Hier findest du einen schnellen Überblick über unsere Formate und kommende Termine.
                </p>
            </section>

            {infoSections.map((section) => (
                <EventFormatSection key={section.id} section={section} events={visibleUpcomingEvents.filter(event => event.eventType === section.id)} />
            ))}

            <section className="events-calendar-section" aria-labelledby="event-calendar-title">
                <h2 id="event-calendar-title">Weitere kommende Events</h2>
                <UpcomingEventGrid events={otherEvents} />
            </section>
        </div>
    );
}
