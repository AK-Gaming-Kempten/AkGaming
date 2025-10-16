import EventCard from "../components/events/EventCard";
import "./Events.css";
import EventInfoTabs from "../components/events/EventInfoTabs";
import {loadPosts} from "../data/loadPosts";
import { Event } from "../data/types";
import {useEffect, useState} from "react";

export default function Events() {
    const infoSections = [
        {
            id: "gamenight",
            title: "Gamenight",
            content:
                "Angefangen als LAN-Party, wurde das Event durch immer neue Angebote erweitert. Diese Entwicklungen führten dazu, dass der Name LAN-Party dem Event nicht mehr gerecht wurde, weshalb es zur Game Night umbenannt wurde.\n" +
                "Auf der Game Night kommen Menschen zusammen, um einen Abend voller Spielspaß zu erleben und die Gemeinschaft zu stärken. Dafür bietet die Game Night ein vielseitiges Programm: Besucher können ihr eigenes Setup mitbringen und im LAN-Netzwerk gemeinsam spielen. Aber auch ohne eigene Hardware gibt es jede Menge zu erleben: Von Konsolen-Angeboten, einem dedizierten Brettspielraum mit Spielen aus der Heldenschmiede, über einen VR-Raum, gecastete Turniere, Projektpräsentationen, Karaoke bis hin zu Special Events – für jeden Besucher lohnt sich der Abend!",
            album: "eventInfoGallery/gamenight",
        },
        {
            id: "gamejam",
            title: "Game Jam",
            content:
                "Die Game Jam ist eine Veranstaltung für jeden der schon immer mal ein Computerspiel entwickeln wollte. Interessierte treffen sich hier um in einem festen Zeitrahmen und einer thematischen Vorgabe ein Spiel zu entwickeln. Die fertigen Spiele werden im Anschluss bewertet und neben der einmaligen Erfahrung winken für tolle Ideen und Umsetzungen auch Preisgelder. Dabei ist jeder erwünscht von 3D Artist, Game Designer oder angehenden Spieleentwickler und oft finden sich dabei auch neue Kontakte. AK Gaming e. V. veranstaltet die Game Jam in Zusammenarbeit mit Professoren der Hochschule Kempten und der Soloplan GmbH zum dritten Mal in Folge. ",
        },
        {
            id: "lol",
            title: "LoL Tournament",
            content:
                "Ist cool. Macht Spaß. Trust.",
            album: "eventInfoGallery/lolTournament",
        },
        {
            id: "boardgames",
            title: "Brettspielabend",
            content:
                "Die Brettspielabende sind vom AK Gaming e. V. und der Heldenschmiede organisierte Termine, an denen sich alles um Brettspiele dreht. Herzlich Willkommen sind alle Spielebegeisterten, die in entspannter Umgebung neue Spiele und neue Leute kennen lernen wollen, oder einfach mit Freunden die 100. Runde ihres Lieblingsspiels spielen wollen.",
        },
    ];

    const [events, setEvents] = useState<Event[]>([]);

    useEffect(() => {
        loadPosts().then((data) => {
            const found = data.filter((p) => p instanceof Event);
            setEvents(found as Event[] ?? null);
        });
    }, []);


    // newest first
    const sortedEvents = [...events].sort(
        (a, b) => new Date(b.startDate).getTime() - new Date(a.startDate).getTime()
    );

    return (
        <main className="events-page">
            <div className="events-page-grid">
                <div className="events-intro">
                    <h1>Über Unsere Events</h1>
                    <EventInfoTabs sections={infoSections} />
                </div>

                <div className="events-feed">
                    <h1>Eventkalender</h1>
                    <section className="events-list">
                        {sortedEvents.map((e) => (
                            <EventCard key={e.id} event={e} />
                        ))}
                    </section>
                </div>
            </div>
        </main>
    );
}
