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
                        Unser Verein ist aus einem Arbeitskreis der Hochschule Kempten hervorgegangen und bietet spielebegeisterten Studierenden Events aus Gebieten wie E-Sports, analogen Spielen und Veranstaltungen rund ums Gaming, um diese Kultur zu stärken und die Gemeinschaft zu fördern.
                    </p>
                    <p>
                        Beispielsweise organisieren wir Game Nights an der Hochschule. Durch die gemeinsamen Abende können sich viele neue Bekanntschaften zwischen den Studierenden bilden. Mehr Infos dazu unter: Game Night.
                        Auch Public Viewing zu aktuellen Online-Events tragen zum studentischen Austausch bei.
                    </p>
                    <p>
                        Als Spieler in einem unserer eSports-Teams lernt man zudem Studierende aus ganz Deutschland kennen, die ebenfalls unsere Leidenschaft fürs Gaming teilen. Hier kann man die eigene Fähigkeit unter Beweis stellen und als eSportler durchstarten.
                    </p>
                    <p>
                        Bei uns gibt es Programm für jeden, der weiß, dass der Kuchen eine Lüge ist.
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
