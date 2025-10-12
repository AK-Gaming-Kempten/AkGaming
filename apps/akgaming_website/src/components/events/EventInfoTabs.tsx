import { useState } from "react";
import "./EventInfoTabs.css";

interface InfoSection {
    id: string;
    title: string;
    content: string;
}

interface EventInfoTabsProps {
    sections: InfoSection[];
}

export default function EventInfoTabs({ sections }: EventInfoTabsProps) {
    const [activeId, setActiveId] = useState(sections[0]?.id ?? "");

    const activeSection = sections.find((s) => s.id === activeId);

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
                </div>
            )}
        </div>
    );
}
