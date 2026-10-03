"use client";

import { useEffect, useState } from "react";
import { LuPause, LuPlay } from "react-icons/lu";
import "./HeroPhotoRotator.css";

interface HeroPhoto {
    src: string;
    alt: string;
}

export default function HeroPhotoRotator({ photos }: { photos: HeroPhoto[] }) {
    const [activeIndex, setActiveIndex] = useState(0);
    const [paused, setPaused] = useState(false);

    useEffect(() => {
        const preference = window.matchMedia("(prefers-reduced-motion: reduce)");
        const updatePreference = () => setPaused(preference.matches);
        updatePreference();
        preference.addEventListener("change", updatePreference);
        return () => preference.removeEventListener("change", updatePreference);
    }, []);

    useEffect(() => {
        if (paused || photos.length < 2) {
            return;
        }
        const timer = window.setInterval(() => {
            if (!document.hidden) {
                setActiveIndex((index) => (index + 1) % photos.length);
            }
        }, 5500);
        return () => window.clearInterval(timer);
    }, [paused, photos.length]);

    return (
        <div className="home-hero-image" role="region" aria-label="Einblicke in AK Gaming">
            <div className="hero-photo-frame">
                {photos.map((photo, index) => (
                    <img
                        key={photo.src}
                        src={photo.src}
                        alt={photo.alt}
                        aria-hidden={index !== activeIndex}
                        className={`hero-photo-slide${index === activeIndex ? " is-active" : ""}`}
                        width={1400}
                        height={933}
                        fetchPriority={index === 0 ? "high" : "auto"}
                    />
                ))}
                <div className="hero-photo-controls">
                    {photos.map((photo, index) => (
                        <button
                            key={photo.src}
                            type="button"
                            className="hero-photo-select"
                            aria-label={`Bild ${index + 1}: ${photo.alt}`}
                            aria-pressed={index === activeIndex}
                            onClick={() => { setActiveIndex(index); setPaused(true); }}
                        />
                    ))}
                    <button
                        type="button"
                        className="hero-photo-pause"
                        aria-label={paused ? "Bildrotation starten" : "Bildrotation pausieren"}
                        onClick={() => setPaused((value) => !value)}
                    >
                        {paused ? <LuPlay /> : <LuPause />}
                    </button>
                </div>
            </div>
            <span className="home-hero-image-label">Gaming und Gemeinschaft in Kempten.</span>
        </div>
    );
}
