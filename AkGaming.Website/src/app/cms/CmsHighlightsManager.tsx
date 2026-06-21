"use client";

import { useEffect, useState } from "react";
import { LuArrowDown, LuArrowUp, LuImagePlus, LuPlus, LuSave, LuTrash2 } from "react-icons/lu";

type CmsPost = {
    id: string;
    title: string;
    shortDescription: string;
};

type Highlight = {
    postId: string;
    mediaSrc: string;
    mediaType: "image" | "video";
    title?: string;
    description?: string;
};

type CmsHighlightsManagerProps = {
    posts: CmsPost[];
};

const emptyHighlight = (): Highlight => ({ postId: "", mediaSrc: "", mediaType: "image" });

export default function CmsHighlightsManager({ posts }: CmsHighlightsManagerProps) {
    const [highlights, setHighlights] = useState<Highlight[]>([]);
    const [selectedIndex, setSelectedIndex] = useState<number | null>(null);
    const [message, setMessage] = useState("");

    useEffect(() => {
        void loadHighlights();
    }, []);

    async function loadHighlights() {
        const response = await fetch("/api/cms/highlights");
        const result = await response.json() as Highlight[] | { message?: string };
        if (!response.ok) {
            setMessage("message" in result ? result.message ?? "Could not load highlights." : "Could not load highlights.");
            return;
        }

        const loaded = result as Highlight[];
        setHighlights(loaded);
        setSelectedIndex(current => current ?? (loaded.length > 0 ? 0 : null));
    }

    function selectHighlight(index: number) {
        setSelectedIndex(index);
        setMessage("");
    }

    function addHighlight() {
        setHighlights(current => [...current, emptyHighlight()]);
        setSelectedIndex(highlights.length);
    }

    function update(field: keyof Highlight, value: string) {
        if (selectedIndex === null) return;

        setHighlights(current => current.map((highlight, index) => index === selectedIndex
            ? { ...highlight, [field]: value }
            : highlight));
    }

    function removeHighlight() {
        if (selectedIndex === null) return;

        setHighlights(current => current.filter((_, index) => index !== selectedIndex));
        setSelectedIndex(current => current === null ? null : Math.max(0, Math.min(current, highlights.length - 2)));
    }

    function moveHighlight(direction: -1 | 1) {
        if (selectedIndex === null) return;
        const targetIndex = selectedIndex + direction;
        if (targetIndex < 0 || targetIndex >= highlights.length) return;

        setHighlights(current => {
            const next = [...current];
            [next[selectedIndex], next[targetIndex]] = [next[targetIndex], next[selectedIndex]];
            return next;
        });
        setSelectedIndex(targetIndex);
    }

    async function saveHighlights() {
        const response = await fetch("/api/cms/highlights", {
            method: "PUT",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(highlights),
        });
        const result = await response.json() as Highlight[] | { message?: string };
        if (!response.ok) {
            setMessage("message" in result ? result.message ?? "Could not save highlights." : "Could not save highlights.");
            return;
        }

        setHighlights(result as Highlight[]);
        setMessage("Highlights saved.");
    }

    const selected = selectedIndex === null ? null : highlights[selectedIndex];
    const selectedPost = selected ? posts.find(post => post.id === selected.postId) : undefined;

    return (
        <div className="cms-highlights-manager">
            <header className="cms-workspace-header cms-highlights-header">
                <div>
                    <p className="cms-eyebrow">Homepage</p>
                    <h2>Highlights</h2>
                </div>
                <div className="cms-highlights-actions">
                    <button type="button" onClick={addHighlight}><LuPlus /> New highlight</button>
                    <button type="button" onClick={() => void saveHighlights()}><LuSave /> Save highlights</button>
                </div>
            </header>

            {message && <p className="cms-editor-message">{message}</p>}
            <div className="cms-highlights-workspace">
                <aside className="cms-highlights-list">
                    {highlights.length === 0 ? <p>No highlights yet.</p> : highlights.map((highlight, index) => (
                        <button key={`${highlight.postId}-${index}`} type="button" className={selectedIndex === index ? "active" : ""} onClick={() => selectHighlight(index)}>
                            <span>{highlight.title || posts.find(post => post.id === highlight.postId)?.title || "Untitled highlight"}</span>
                            <small>{highlight.postId || "Choose a post"}</small>
                        </button>
                    ))}
                </aside>
                <section className="cms-highlight-editor">
                    {selected === null ? <p className="cms-highlight-empty"><LuImagePlus /> Select or create a highlight.</p> : <>
                        <div className="cms-form-grid">
                            <label className="wide">Post<select value={selected.postId} onChange={event => update("postId", event.target.value)}><option value="">Choose a post…</option>{posts.map(post => <option key={post.id} value={post.id}>{post.title}</option>)}</select></label>
                            <label>Media type<select value={selected.mediaType} onChange={event => update("mediaType", event.target.value)}><option value="image">Image</option><option value="video">Video</option></select></label>
                            <label>Media URL<input value={selected.mediaSrc} onChange={event => update("mediaSrc", event.target.value)} placeholder="/media/image.png" /></label>
                            <label className="wide">Title override<input value={selected.title ?? ""} onChange={event => update("title", event.target.value)} placeholder={selectedPost?.title ?? "Uses the post title"} /></label>
                            <label className="wide">Description override<textarea value={selected.description ?? ""} onChange={event => update("description", event.target.value)} placeholder={selectedPost?.shortDescription ?? "Uses the post description"} /></label>
                        </div>
                        <div className="cms-highlight-order-actions">
                            <button type="button" onClick={() => moveHighlight(-1)} disabled={selectedIndex === 0} title="Move up"><LuArrowUp /> Move up</button>
                            <button type="button" onClick={() => moveHighlight(1)} disabled={selectedIndex === highlights.length - 1} title="Move down"><LuArrowDown /> Move down</button>
                            <button type="button" className="cms-secondary-button" onClick={removeHighlight}><LuTrash2 /> Remove</button>
                        </div>
                        <article className="cms-highlight-preview">
                            {selected.mediaType === "video" ? <video src={selected.mediaSrc} controls /> : selected.mediaSrc ? <img src={selected.mediaSrc} alt="Highlight preview" /> : <div className="cms-highlight-preview-placeholder"><LuImagePlus /></div>}
                            <div>
                                <h3>{selected.title || selectedPost?.title || "Highlight title"}</h3>
                                <p>{selected.description || selectedPost?.shortDescription || "Highlight description"}</p>
                            </div>
                        </article>
                    </>}
                </section>
            </div>
        </div>
    );
}
