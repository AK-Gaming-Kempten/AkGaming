"use client";

import { useCallback, useEffect, useState } from "react";
import { LuImagePlus, LuPlus, LuSave } from "react-icons/lu";
import CmsTeamsManager from "./CmsTeamsManager";

type Tab = "games" | "leagues" | "teams";
type Game = { id: string; displayName: string; logo: string };
type League = { id: string; name: string; logo: string };
type CatalogItem = { id: string; label: string; logo: string };

export default function CmsEsportsManager() {
    const [tab, setTab] = useState<Tab>("teams");
    const [games, setGames] = useState<Game[]>([]);
    const [leagues, setLeagues] = useState<League[]>([]);

    useEffect(() => {
        void Promise.all([loadCatalog<Game>("games"), loadCatalog<League>("leagues")]).then(([loadedGames, loadedLeagues]) => {
            if (loadedGames !== null) setGames(loadedGames);
            if (loadedLeagues !== null) setLeagues(loadedLeagues);
        });
    }, []);

    return (
        <div className="cms-esports-manager">
            <header className="cms-workspace-header cms-esports-header">
                <div>
                    <p className="cms-eyebrow">Content</p>
                    <h2>Esports</h2>
                </div>
            </header>
            <nav className="cms-esports-tabs" aria-label="Esports sections">
                <button type="button" className={tab === "teams" ? "active" : ""} onClick={() => setTab("teams")}>Teams</button>
                <button type="button" className={tab === "leagues" ? "active" : ""} onClick={() => setTab("leagues")}>Leagues</button>
                <button type="button" className={tab === "games" ? "active" : ""} onClick={() => setTab("games")}>Games</button>
            </nav>
            {tab === "teams" ? <CmsTeamsManager games={games} leagues={leagues} /> : <CmsEsportsCatalogManager kind={tab} onChanged={() => void refreshCatalog(tab)} />}
        </div>
    );

    async function refreshCatalog(kind: "games" | "leagues") {
        const values = await loadCatalog<Game | League>(kind);
        if (values === null) return;
        if (kind === "games") setGames(values as Game[]);
        else setLeagues(values as League[]);
    }
}

type CmsEsportsCatalogManagerProps = {
    kind: "games" | "leagues";
    onChanged: () => void;
};

function CmsEsportsCatalogManager({ kind, onChanged }: CmsEsportsCatalogManagerProps) {
    const isGame = kind === "games";
    const [items, setItems] = useState<CatalogItem[]>([]);
    const [selectedIndex, setSelectedIndex] = useState<number | null>(null);
    const [message, setMessage] = useState("");

    const loadItems = useCallback(async () => {
        const values = await loadCatalog<Game | League>(kind);
        if (values === null) return;

        const catalogItems = values.map(value => ({ id: value.id, label: isGame ? (value as Game).displayName : (value as League).name, logo: value.logo }));
        setItems(catalogItems);
        setSelectedIndex(catalogItems.length > 0 ? 0 : null);
    }, [isGame, kind]);

    useEffect(() => {
        void loadItems();
    }, [loadItems]);

    function update(field: keyof CatalogItem, value: string) {
        if (selectedIndex === null) return;
        setItems(current => current.map((item, index) => index === selectedIndex ? { ...item, [field]: value } : item));
    }

    function addItem() {
        setItems(current => [...current, { id: "", label: "", logo: "/media/" }]);
        setSelectedIndex(items.length);
    }

    async function saveItems() {
        const payload = items.map(item => isGame
            ? { id: item.id, displayName: item.label, logo: item.logo }
            : { id: item.id, name: item.label, logo: item.logo });
        const response = await fetch(`/api/cms/esports/${kind}`, {
            method: "PUT",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload),
        });
        const result = await response.json() as unknown[] | { message?: string };
        if (!response.ok) {
            setMessage("message" in result ? result.message ?? "Could not save catalog." : "Could not save catalog.");
            return;
        }

        setMessage(`${isGame ? "Games" : "Leagues"} saved.`);
        await loadItems();
        onChanged();
    }

    const selected = selectedIndex === null ? null : items[selectedIndex];
    const singular = isGame ? "game" : "league";
    const title = isGame ? "Games" : "Leagues";

    return (
        <div className="cms-esports-catalog">
            <header className="cms-workspace-header cms-esports-catalog-header">
                <h3>{title}</h3>
                <div className="cms-teams-actions"><button type="button" onClick={addItem}><LuPlus /> New {singular}</button><button type="button" onClick={() => void saveItems()}><LuSave /> Save {title.toLowerCase()}</button></div>
            </header>
            {message && <p className="cms-editor-message">{message}</p>}
            <div className="cms-teams-workspace">
                <aside className="cms-teams-list">
                    {items.map((item, index) => <button key={`${item.id}-${index}`} type="button" className={selectedIndex === index ? "active" : ""} onClick={() => setSelectedIndex(index)}>{item.logo && <img src={item.logo} alt="" />}<span>{item.label || `Untitled ${singular}`}<small>{item.id || "Set ID"}</small></span></button>)}
                </aside>
                <section className="cms-team-editor">
                    {selected === null ? <p className="cms-team-empty"><LuImagePlus /> Select or create a {singular}.</p> : <>
                        <div className="cms-form-grid">
                            <label>ID<input value={selected.id} onChange={event => update("id", event.target.value)} placeholder={`new-${singular}`} /></label>
                            <label>{isGame ? "Display name" : "League name"}<input value={selected.label} onChange={event => update("label", event.target.value)} /></label>
                            <label className="wide">Logo URL<input value={selected.logo} onChange={event => update("logo", event.target.value)} placeholder="/media/logo.png" /></label>
                        </div>
                        <div className="cms-team-preview">{selected.logo ? <img src={selected.logo} alt="Catalog logo preview" /> : <LuImagePlus />}<div><h3>{selected.label || title}</h3><p>{selected.id || "ID"}</p></div></div>
                    </>}
                </section>
            </div>
        </div>
    );
}

async function loadCatalog<T>(kind: "games" | "leagues"): Promise<T[] | null> {
    const response = await fetch(`/api/cms/esports/${kind}`);
    if (!response.ok) return null;
    return response.json() as Promise<T[]>;
}
