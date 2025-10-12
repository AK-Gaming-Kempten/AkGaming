import EventCard from "../components/events/EventCard";
import "./Events.css";
import EventInfoTabs from "../components/events/EventInfoTabs";

export default function Events() {
    const infoSections = [
        {
            id: "gamenight",
            title: "Gamenight",
            content:
                "Angefangen als LAN-Party, wurde das Event durch immer neue Angebote erweitert. Diese Entwicklungen führten dazu, dass der Name LAN-Party dem Event nicht mehr gerecht wurde, weshalb es zur Game Night umbenannt wurde.\n" +
                "Auf der Game Night kommen Menschen zusammen, um einen Abend voller Spielspaß zu erleben und die Gemeinschaft zu stärken. Dafür bietet die Game Night ein vielseitiges Programm: Besucher können ihr eigenes Setup mitbringen und im LAN-Netzwerk gemeinsam spielen. Aber auch ohne eigene Hardware gibt es jede Menge zu erleben: Von Konsolen-Angeboten, einem dedizierten Brettspielraum mit Spielen aus der Heldenschmiede, über einen VR-Raum, gecastete Turniere, Projektpräsentationen, Karaoke bis hin zu Special Events – für jeden Besucher lohnt sich der Abend!",
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
        },
        {
            id: "boardgames",
            title: "Brettspielabend",
            content:
                "Die Brettspielabende sind vom AK Gaming e. V. und der Heldenschmiede organisierte Termine, an denen sich alles um Brettspiele dreht. Herzlich Willkommen sind alle Spielebegeisterten, die in entspannter Umgebung neue Spiele und neue Leute kennen lernen wollen, oder einfach mit Freunden die 100. Runde ihres Lieblingsspiels spielen wollen.",
        },
    ];

    const events = [
        {
            id: "gamenight-5",
            name: "Game Night Oktober 2025",
            dateStart: "2025-10-25",
            dateEnd: "2025-10-26",
            description:
                "Ein neues Semester hat begonnen und nun ist es endlich wieder soweit: Am Samstag den 25. Oktober. " +
                "AK Gaming e.V. lädt ein zur ersten Game Night im Wintersemester 25/26, im S-Gebäude der Hochschule " +
                "Kempten. Während draußen die Blätter fallen, fallen hier die Würfel. Es erwartet euch wieder ein Abend " +
                "voller Brettspiele, aufregenden Turnieren und langen Stunden mit jede Menge Videogames.",
            locationName: "Hochschule Kempten, S-Bau",
            locationUrl: "https://maps.app.goo.gl/QLkFDmV9jni5X28R8",
        },
        {
            id: "lol-tournament-6",
            name: "LoL Turnier #6",
            dateStart: "2025-11-22",
            description: "Turnier - Wird Cool! OwO",
            locationName: "Online: Discord",
            locationUrl: "https://discord.gg/rCXy4pYYtG",
        },
        {
            id: "game-jam-5",
            name: "Game Jam #5",
            dateStart: "2025-12-05",
            dateEnd: "2025-12-12",
            description: "Game Jam.",
            locationName: "Online: Itch.io",
            locationUrl: "https://itch.io",
        },
    ];

    // newest first
    const sortedEvents = [...events].sort(
        (a, b) => new Date(b.dateStart).getTime() - new Date(a.dateStart).getTime()
    );

    return (
        <main className="events-page">
            <h1>Über Unsere Events</h1>

            <EventInfoTabs sections={infoSections} />

            <h1>Eventkalender</h1>
            <section className="events-feed">
                {sortedEvents.map((ev) => (
                    <EventCard key={ev.id} {...ev} />
                ))}
            </section>
        </main>
    );
}
