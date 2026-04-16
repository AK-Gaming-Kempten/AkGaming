import "./Home.css";

import HighlightCard from "../components/home/HighlightCard";
import SponsorCard from "../components/home/SponsorCard";
import SocialLinks from "../components/home/SocialLinks";
import MiniCalendar from "../components/home/MiniCalendar";

import { useEffect, useMemo, useRef, useState, type TransitionEvent } from "react";
import { loadHighlights } from "../data/loadHighlights";
import type { Highlight } from "../data/types";
import { Link } from "react-router-dom";
import heroLogo from "../../public/assets/akgaming_logo.png";

export default function Home() {
    const [highlights, setHighlights] = useState<Highlight[]>([]);
    const [carouselIndex, setCarouselIndex] = useState(0);
    const [carouselOffset, setCarouselOffset] = useState(0);
    const [isCarouselAnimating, setIsCarouselAnimating] = useState(true);
    const viewportRef = useRef<HTMLDivElement | null>(null);
    const slideRefs = useRef<Array<HTMLDivElement | null>>([]);

    useEffect(() => {
        loadHighlights().then(setHighlights);
    }, []);

    const rotatingHighlights = useMemo(() => highlights, [highlights]);
    const isCarouselEnabled = rotatingHighlights.length > 2;
    const carouselPages = Math.max(1, rotatingHighlights.length);
    const carouselHighlights = useMemo(() => {
        if (!isCarouselEnabled) {
            return rotatingHighlights;
        }

        return [
            ...rotatingHighlights,
            ...rotatingHighlights.slice(0, 2),
        ];
    }, [isCarouselEnabled, rotatingHighlights]);

    useEffect(() => {
        if (!isCarouselEnabled) {
            return;
        }

        const updateOffset = () => {
            const nextOffset = slideRefs.current[carouselIndex]?.offsetLeft ?? 0;
            setCarouselOffset(nextOffset);
        };

        updateOffset();

        const viewport = viewportRef.current;
        if (!viewport) {
            return;
        }

        const observer = new ResizeObserver(() => {
            updateOffset();
        });

        observer.observe(viewport);

        return () => observer.disconnect();
    }, [carouselIndex, carouselHighlights, isCarouselEnabled]);

    useEffect(() => {
        if (!isCarouselEnabled) {
            return;
        }

        const intervalId = window.setInterval(() => {
            setIsCarouselAnimating(true);
            setCarouselIndex((prev) => prev + 1);
        }, 4500);

        return () => window.clearInterval(intervalId);
    }, [isCarouselEnabled]);

    useEffect(() => {
        if (isCarouselEnabled) {
            return;
        }

        setCarouselIndex(0);
        setCarouselOffset(0);
        setIsCarouselAnimating(true);
    }, [isCarouselEnabled]);

    const activePage = carouselIndex % carouselPages;

    const handleCarouselTransitionEnd = (event: TransitionEvent<HTMLDivElement>) => {
        if (event.target !== event.currentTarget || event.propertyName !== "transform") {
            return;
        }

        if (!isCarouselEnabled || carouselIndex < rotatingHighlights.length) {
            return;
        }

        setIsCarouselAnimating(false);
        setCarouselIndex((prev) => prev - rotatingHighlights.length);

        window.requestAnimationFrame(() => {
            window.requestAnimationFrame(() => {
                setIsCarouselAnimating(true);
            });
        });
    };

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
                {rotatingHighlights.length === 0 ? (
                    <p className="home-empty">Derzeit sind keine Highlights verfügbar.</p>
                ) : !isCarouselEnabled ? (
                    <div className="highlight-list">
                        {rotatingHighlights.map((h) => (
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
                ) : (
                    <div className="home-highlight-rotator">
                        <div className="home-highlight-viewport" ref={viewportRef}>
                            <div
                                className={`home-highlight-track ${isCarouselAnimating ? "is-animating" : ""}`}
                                style={{ transform: `translate3d(-${carouselOffset}px, 0, 0)` }}
                                onTransitionEnd={handleCarouselTransitionEnd}
                            >
                                {carouselHighlights.map((h, index) => (
                                    <div
                                        key={`${h.postId}-${index}`}
                                        className="home-highlight-slide"
                                        ref={(node) => {
                                            slideRefs.current[index] = node;
                                        }}
                                    >
                                        <HighlightCard
                                            title={h.title ?? ""}
                                            description={h.description ?? ""}
                                            mediaSrc={h.mediaSrc}
                                            mediaType={h.mediaType}
                                            postId={h.postId}
                                        />
                                    </div>
                                ))}
                            </div>
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
