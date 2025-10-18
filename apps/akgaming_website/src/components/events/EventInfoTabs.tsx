import { useEffect, useState } from "react";
import PhotoAlbum, { type Photo, } from "react-photo-album";
import { loadImages } from "../../data/loadImages";
import "react-photo-album/styles.css";

import "./EventInfoTabs.css";

export interface InfoSection {
    id: string;
    title: string;
    content: string;
    album?: string; // e.g. "lan2024"
}

interface EventInfoTabsProps {
    sections: InfoSection[];
}

export default function EventInfoTabs({ sections }: EventInfoTabsProps) {
    const [activeId, setActiveId] = useState(sections[0]?.id ?? "");
    const [photos, setPhotos] = useState<Photo[]>([]);

    const activeSection = sections.find((s) => s.id === activeId);

    useEffect(() => {
        if (!activeSection?.album) {
            setPhotos([]);
            return;
        }

        // ✅ loadImages only needs the folder name, e.g. "lan2024"
        loadImages(activeSection.album, { includeDimensions: true }).then((imgs) => {
            // Map LoadedImage[] → Photo[] for react-photo-album
            const formatted: Photo[] = imgs.map((img) => ({
                src: img.src,
                width: img.width ?? 800,
                height: img.height ?? 600,
            }));
            setPhotos(formatted);
        });
    }, [activeSection]);

    return (
        <div className="event-info-tabs">
            <nav className="info-nav">
                {sections.map((section) => (
                    <button
                        key={section.id}
                        className={`info-tab ${activeId === section.id ? "active" : ""}`}
                        onClick={() => setActiveId(section.id)}
                    >
                        {section.title}
                    </button>
                ))}
            </nav>

            {activeSection && (
                <div className="info-content">
                    <p>{activeSection.content}</p>

                    {photos.length > 0 && (
                        <div className="info-gallery">
                            <PhotoAlbum
                                photos={photos}
                                layout="rows"
                                skeleton={<div style={{ width: "100%", minHeight: 800 }} />}
                            />
                        </div>
                    )}
                </div>
            )}
        </div>
    );
}

