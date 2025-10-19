import "./Home.css";

import HighlightCard from "../components/home/HighlightCard";
import SponsorCard from "../components/home/SponsorCard";
import SocialLinks from "../components/home/SocialLinks";
import MiniCalendar from "../components/home/MiniCalendar";

import { useEffect, useState } from "react";
import { loadHighlights } from "../data/loadHighlights";
import type { Highlight } from "../data/types";

export default function Home() {
    const [highlights, setHighlights] = useState<Highlight[]>([]);

    useEffect(() => {
        loadHighlights().then(setHighlights);
    }, []);
    return (
        <main className="home-page">
            <div className="home-grid">
                {/* Left column */}
                <aside className="home-left">
                    <h2>Vereinsleben</h2>
                    <div className="highlight-list">
                        {highlights.map((h) => (
                            <HighlightCard
                                key={h.postId}
                                title={h.title ?? ""}
                                description={h.description ?? ""}
                                mediaSrc={h.mediaSrc}
                                mediaType={h.mediaType}
                                postId={h.postId}
                            />
                        ))}
                    </div>
                </aside>

                {/* Center column */}
                <section className="home-center">
                    <h1>Gaming at its best</h1>
                    <p>
                        Der AK Gaming e.V. setzt sich für die Förderung der Gaming-Kultur in Kempten ein. Dazu gehören für uns lokale Events mit Fokus auf zwischenmenschliche Vernetzung, Online-Events bei denen wir den Zusammenhalt der gesamten Gaming-Community in Deutschland stärken und Engagement im E-Sports indem dem wir Talente fördern und Hobby-Sportlern einen einfachen Einstieg in die Szene bieten. Im Vordergrund stehen bei uns Menschen und das was wir für Spaß und ein starkes Miteinander in der Gaming-Community tun können.
                    </p>
                    <p>
                        <h3>Ursprung</h3>
                        Der AK Gaming e.V. ist aus dem gleichnamigen Arbeitskreis der Hochschule Kempten hervorgegangen und arbeitet weiter eng mit der Hochschule und der Fakultät für Informatik zusammen. So finden unsere Live-Events meist direkt an der Hochschule statt und richten sich insbesondere an die Studenten und bei Online-Events dürfen wir regelmäßig Professoren der Hochschule begrüßen, wie etwa als Jury beim Ak Gaming Game Jam.
                    </p>
                    <p>
                        <h3>Programm</h3>
                        Wir bieten regelmäßig Online sowie Offline-Events an, die unsere Vision einer starken lokalen sowie nationalen Community im Gaming-Bereich verfolgen. Unser Aushängeschild ist dabei die 4 mal jährlich stattfindende Game-Night, auf der oft über hundert Gaming-Begeisterte zusammenkommen und bei der von Brettspielen über LAN-Games und Turniere bis zu VR und Karaoke für so ziemlich jeden was dabei ist. Dazu veranstalten wir regelmäßige offene Brettspielabende, deutschlandweite Online-Turniere und Game-Jams.
                    </p>
                    <p>
                        <h3>E-Sports</h3>

                        Im E-Sports-Bereich fördern wir gezielt Talente aus der Region und bieten motivierten Spielern die Möglichkeit, erste Turniererfahrungen zu sammeln. Unser Ziel ist es, sowohl ambitionierten Nachwuchstalenten als auch Hobby-Gamern eine Plattform zu geben, auf der sie sich weiterentwickeln, vernetzen und gemeinsam Erfolge feiern können. Neben den zahlreichen Teams, die wir betreuen, organisieren wir regelmäßig selbst Turniere und engagieren uns für eine großräumige Vernetzung als Mitglied des <a href="https://esport.bayern/" target="_blank">Esport Verbands Bayern</a>. Dabei stehen Fairness, Teamgeist und Freude am Spiel bei uns immer an erster Stelle.
                    </p>
                </section>

                {/* Right column */}
                <aside className="home-right">
                    <SponsorCard />
                    <SocialLinks />
                    <MiniCalendar />
                </aside>
            </div>
        </main>
    );
}
