import "./Home.css";

import HighlightCard from "../components/home/HighlightCard";
import SponsorCard from "../components/home/SponsorCard";
import SocialLinks from "../components/home/SocialLinks";
import MiniCalendar from "../components/home/MiniCalendar";

import { useEffect, useMemo, useState } from "react";
import { loadHighlights } from "../data/loadHighlights";
import type { Highlight } from "../data/types";
import { Link } from "react-router-dom";
import heroLogo from "../../public/assets/akgaming_logo.png";

export default function Home() {
    const [highlights, setHighlights] = useState<Highlight[]>([]);
    const [carouselIndex, setCarouselIndex] = useState(0);

    useEffect(() => {
        loadHighlights().then(setHighlights);
    }, []);

    const rotatingHighlights = useMemo(() => highlights, [highlights]);

    const carouselPages = Math.max(1, rotatingHighlights.length);

    const visibleHighlights = useMemo(() => {
        if (rotatingHighlights.length === 0) {
            return [];
        }
        if (rotatingHighlights.length <= 2) {
            return rotatingHighlights;
        }

        const first = rotatingHighlights[carouselIndex % rotatingHighlights.length];
        const second = rotatingHighlights[(carouselIndex + 1) % rotatingHighlights.length];
        return [first, second];
    }, [carouselIndex, rotatingHighlights]);

    useEffect(() => {
        if (rotatingHighlights.length <= 2) {
            return;
        }

        const intervalId = window.setInterval(() => {
            setCarouselIndex((prev) => (prev + 1) % rotatingHighlights.length);
        }, 4500);

        return () => window.clearInterval(intervalId);
    }, [rotatingHighlights.length]);

    const activePage = carouselIndex % carouselPages;

    return (
        <main className="home-page">
            <section className="home-hero">
                <div className="home-hero-main">
                    <div>
                        <p className="home-hero-eyebrow">AK Gaming e.V. Kempten</p>
                        <h1>Gaming at its best</h1>
                        <p className="home-hero-copy">
                            Wir verbinden Community, Events und E-Sports zu einem Vereinsleben, in dem
                            Fairness, Teamgeist und Spaß im Mittelpunkt stehen.
                        </p>
                        <div className="home-hero-actions">
                            <Link to="/events" className="home-btn home-btn-primary">Events entdecken</Link>
                            <Link to="/esports" className="home-btn home-btn-secondary">E-Sports Teams</Link>
                        </div>
                    </div>
                    <div className="home-hero-logo-wrap">
                        <img src={heroLogo} alt="AK Gaming Logo" className="home-hero-logo" />
                    </div>
                </div>
            </section>

            <section className="home-section home-section-gradient">
                <div className="home-section-title-row">
                    <h2>Über uns</h2>
                </div>
                <div className="home-info-grid">
                    <article className="home-info-card">
                        <h3>Mission</h3>
                        <p>
                            Der AK Gaming e.V. setzt sich für die Förderung der Gaming-Kultur in
                            Kempten ein. Dazu gehören lokale Events mit Fokus auf Vernetzung,
                            Online-Events für Zusammenhalt in der gesamten Community und Engagement
                            im E-Sports mit niedrigschwelligem Einstieg.
                        </p>
                    </article>
                    <article className="home-info-card">
                        <h3>Ursprung</h3>
                        <p>
                            Der Verein ist aus dem gleichnamigen Arbeitskreis der Hochschule Kempten
                            hervorgegangen. Wir arbeiten weiterhin eng mit der Hochschule und der
                            Fakultät für Informatik zusammen, dadurch finden viele Live-Events direkt
                            vor Ort statt.
                        </p>
                    </article>
                    <article className="home-info-card">
                        <h3>Programm</h3>
                        <p>
                            Wir veranstalten regelmäßig Online- und Offline-Events. Unser
                            Aushängeschild ist die mehrmals jährlich stattfindende Game-Night mit
                            Brettspielen, LAN-Games, Turnieren, VR und Karaoke sowie offene
                            Brettspielabende, Online-Turniere und Game-Jams.
                        </p>
                    </article>
                    <article className="home-info-card">
                        <h3>E-Sports</h3>
                        <p>
                            Wir fördern Talente aus der Region und bieten ambitionierten sowie
                            Hobby-Spielern eine Plattform zur Weiterentwicklung. Als Mitglied des{" "}
                            <a href="https://esport.bayern/" target="_blank" rel="noreferrer">
                                Esport Verbands Bayern
                            </a>{" "}
                            engagieren wir uns für faire und nachhaltige Strukturen.
                        </p>
                    </article>
                </div>
            </section>

            <section className="home-section">
                <div className="home-section-title-row">
                    <h2>Vereinsleben</h2>
                    <Link to="/events" className="home-link-action">Alle Events</Link>
                </div>
                {visibleHighlights.length === 0 ? (
                    <p className="home-empty">Derzeit sind keine Highlights verfügbar.</p>
                ) : (
                    <div className="home-highlight-rotator">
                        <div className="highlight-list">
                            {visibleHighlights.map((h) => (
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
                        {carouselPages > 1 && (
                            <div className="home-carousel-dots" aria-hidden="true">
                                {Array.from({ length: carouselPages }).map((_, index) => (
                                    <span
                                        key={`carousel-dot-${index}`}
                                        className={`home-carousel-dot ${index === activePage ? "active" : ""}`}
                                    />
                                ))}
                            </div>
                        )}
                    </div>
                )}
            </section>

            <section className="home-section">
                <div className="home-section-title-row">
                    <h2>Quicklinks</h2>
                </div>
                <div className="home-quicklinks-grid">
                    <div className="home-panel">
                        <SponsorCard />
                    </div>
                    <div className="home-panel">
                        <h3 className="home-panel-title">Folge uns</h3>
                        <SocialLinks />
                    </div>
                    <div className="home-panel">
                        <MiniCalendar />
                    </div>
                </div>
            </section>
        </main>
    );
}
