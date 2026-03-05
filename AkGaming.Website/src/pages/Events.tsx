import EventCard from "../components/events/EventCard";
import "./Events.css";
import EventInfoTabs from "../components/events/EventInfoTabs";
import { loadPosts } from "../data/loadPosts";
import { Event } from "../data/types";
import { useEffect, useState } from "react";
import { getVisibleUpcomingEvents } from "../utils/eventDates";

export default function Events() {
    const infoSections = [
        {
            id: "gamenight",
            title: "Gamenight",
            content:
                "Auf der Game Night kommen Menschen zusammen, um einen Abend voller Spielspaß zu erleben und die Gemeinschaft zu stärken. Dafür bietet die Game Night ein vielseitiges Programm: Besucher können ihr eigenes Gaming-Setup mitbringen und im LAN-Netzwerk gemeinsam spielen. Auch ohne eigene Hardware gibt es jede Menge zu erleben: Wir stellen einige moderne sowie Retro-Konsolen, eine riesige Auswahl an Brettspielen, einen VR-Raum, moderierte Turniere mit Sachpreisen, Projektpräsentationen, Karaoke und Special Events – da ist auf jeden Fall für Alle etwas dabei!",
            album: "eventInfoGallery/gamenight",
        },
        {
            id: "gamejam",
            title: "Game Jam",
            content:
                "Unser Game Jam ist eine Veranstaltung für jeden, der schon immer mal ein Computerspiel entwickeln wollte, bereits Profi in der Disziplin ist oder sich dafür interessiert, was dabei rauskommt, wenn jemand in wenigen Tagen so etwas auf die Beine stellt. Interessierte treffen sich hier um in einem festen Zeitrahmen und einer thematischen Vorgabe ein Spiel zu entwickeln. Die fertigen Spiele werden im Anschluss bewertet und neben der einmaligen Erfahrung winken für tolle Ideen und Umsetzungen auch Preisgelder. Egal ob 3D Artist, Game Designer oder Programmierer, als Teil eines kleinen Teams oder ganz alleine kann jeder mitmachen.",
            album: "eventInfoGallery/gamejam",
        },
        {
            id: "boardgames",
            title: "Brettspielabend",
            content:
                "In Zusammenarbeit mit der Heldenschmiede stellen wir regelmäig eine riesige Auswahl an Brettspielen bei unserem Brettspielabend zur verfügung. Hier kann jeder vorbei kommen, Leute kennenlernen, neue Spiele ausprobieren oder die 1000ste Runde seines Lieblingsspiels starten.",
            album: "eventInfoGallery/boardgames",
        },
    ];

    const [events, setEvents] = useState<Event[]>([]);

    useEffect(() => {
        loadPosts().then((data) => {
            const found = data.filter((p) => p instanceof Event);
            setEvents(found as Event[] ?? []);
        });
    }, []);

    const visibleUpcomingEvents = getVisibleUpcomingEvents(events);

    return (
        <main className="events-page">
            <section className="events-hero">
                <p className="events-eyebrow">AK Gaming e.V.</p>
                <h1>Events</h1>
                <p className="events-hero-copy">
                    Lokale und digitale Formate für Community, Wettbewerb und Austausch.
                    Hier findest du einen schnellen Überblick über unsere Formate und kommende Termine.
                </p>
            </section>

            <section className="events-layout">
                <div className="events-intro">
                    <div className="events-section-heading">
                        <h2>Über unsere Events</h2>
                    </div>
                    <EventInfoTabs sections={infoSections} />
                </div>

                <aside className="events-feed">
                    <div className="events-section-heading">
                        <h2>Eventkalender</h2>
                    </div>
                    <section className="events-list">
                        {visibleUpcomingEvents.map((e) => (
                            <EventCard key={e.id} event={e} />
                        ))}
                    </section>
                </aside>
            </section>
        </main>
    );
}
