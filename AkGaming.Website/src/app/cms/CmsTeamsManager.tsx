"use client";

import { useCallback, useEffect, useState } from "react";
import { LuImagePlus, LuPlus, LuSave, LuTrash2, LuUserPlus, LuX } from "react-icons/lu";
import { useCmsToast } from "./CmsToastProvider";

type Player = {
    name: string;
    role: string;
    picture: string;
};

type Team = {
    id: string;
    game: string;
    name: string;
    logo: string;
    leagueId: string;
    division: string;
    players: Player[];
};

type Game = { id: string; displayName: string; logo: string };
type League = { id: string; name: string; logo: string };

type CmsTeamsManagerProps = {
    games: Game[];
    leagues: League[];
};

const emptyTeam = (): Team => ({
    id: "",
    game: "",
    name: "",
    logo: "/media/teams/",
    leagueId: "",
    division: "",
    players: [],
});

export default function CmsTeamsManager({ games, leagues }: CmsTeamsManagerProps) {
    const [teams, setTeams] = useState<Team[]>([]);
    const [selected, setSelected] = useState<Team | null>(null);
    const [previousId, setPreviousId] = useState<string | undefined>();
    const { showToast } = useCmsToast();

    const loadTeams = useCallback(async (): Promise<Team[] | null> => {
        const response = await fetch("/api/cms/teams");
        const result = await response.json() as Team[] | { message?: string };
        if (!response.ok) {
            showToast("message" in result ? result.message ?? "Could not load teams." : "Could not load teams.", "error");
            return null;
        }

        const loadedTeams = result as Team[];
        setTeams(loadedTeams);
        return loadedTeams;
    }, [showToast]);

    useEffect(() => {
        void loadTeams().then(loadedTeams => {
            if (loadedTeams !== null && loadedTeams.length > 0) {
                setSelected(loadedTeams[0]);
                setPreviousId(loadedTeams[0].id);
            }
        });
    }, [loadTeams]);

    function selectTeam(team: Team) {
        setSelected(team);
        setPreviousId(team.id);
    }

    function updateTeam(update: (team: Team) => Team) {
        setSelected(current => current === null ? current : update(current));
    }

    function createTeam() {
        setSelected(emptyTeam());
        setPreviousId(undefined);
    }

    function addPlayer() {
        updateTeam(team => ({ ...team, players: [...team.players, { name: "", role: "", picture: team.logo }] }));
    }

    function updatePlayer(index: number, field: keyof Player, value: string) {
        updateTeam(team => ({
            ...team,
            players: team.players.map((player, playerIndex) => playerIndex === index ? { ...player, [field]: value } : player),
        }));
    }

    function removePlayer(index: number) {
        updateTeam(team => ({ ...team, players: team.players.filter((_, playerIndex) => playerIndex !== index) }));
    }

    async function saveTeam() {
        if (selected === null) return;

        const response = await fetch("/api/cms/teams", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ team: selected, previousId }),
        });
        const result = await response.json() as Team | { message?: string };
        if (!response.ok) {
            showToast("message" in result ? result.message ?? "Could not save team." : "Could not save team.", "error");
            return;
        }

        const savedTeam = result as Team;
        setSelected(savedTeam);
        setPreviousId(savedTeam.id);
        showToast("Team saved.");
        await loadTeams();
    }

    async function deleteTeam() {
        if (selected === null || !previousId) return;
        if (!window.confirm(`Delete '${selected.name}'?`)) return;

        const response = await fetch(`/api/cms/teams/${encodeURIComponent(previousId)}`, { method: "DELETE" });
        if (!response.ok) {
            const result = await response.json() as { message?: string };
            showToast(result.message ?? "Could not delete team.", "error");
            return;
        }

        setSelected(null);
        setPreviousId(undefined);
        await loadTeams();
    }

    const selectedLeague = selected === null ? undefined : leagues.find(league => league.id === selected.leagueId);

    return (
        <div className="cms-teams-manager">
            <header className="cms-workspace-header cms-teams-header">
                <div>
                    <p className="cms-eyebrow">Esports</p>
                    <h2>Teams</h2>
                </div>
                <div className="cms-teams-actions">
                    <button type="button" className="cms-icon-button" onClick={createTeam} title="New team" aria-label="New team"><LuPlus /></button>
                    <button type="button" className="cms-icon-button" onClick={() => void saveTeam()} disabled={selected === null} title="Save team" aria-label="Save team"><LuSave /></button>
                    {previousId && <button type="button" className="cms-icon-button cms-icon-button-danger" onClick={() => void deleteTeam()} title="Delete team" aria-label="Delete team"><LuTrash2 /></button>}
                </div>
            </header>

            <div className="cms-teams-workspace">
                <aside className="cms-teams-list">
                    {teams.map(team => <button key={team.id} type="button" className={selected?.id === team.id ? "active" : ""} onClick={() => selectTeam(team)}><img src={team.logo} alt="" /><span>{team.name}<small>{team.game}</small></span></button>)}
                </aside>
                <section className="cms-team-editor">
                    {selected === null ? <p className="cms-team-empty"><LuImagePlus /> Select or create a team.</p> : <>
                        <div className="cms-form-grid">
                            <label>Team ID<input value={selected.id} onChange={event => updateTeam(team => ({ ...team, id: event.target.value }))} placeholder="hsk-new-team" /></label>
                            <label>Game<select value={selected.game} onChange={event => updateTeam(team => ({ ...team, game: event.target.value }))}><option value="">Choose a game…</option>{games.map(game => <option key={game.id} value={game.id}>{game.displayName}</option>)}</select></label>
                            <label className="wide">Team name<input value={selected.name} onChange={event => updateTeam(team => ({ ...team, name: event.target.value }))} /></label>
                            <label className="wide">Team logo URL<input value={selected.logo} onChange={event => updateTeam(team => ({ ...team, logo: event.target.value }))} placeholder="/media/teams/logo.png" /></label>
                            <label>League<select value={selected.leagueId} onChange={event => updateTeam(team => ({ ...team, leagueId: event.target.value }))}><option value="">Choose a league…</option>{leagues.map(league => <option key={league.id} value={league.id}>{league.name}</option>)}</select></label>
                            <label>League division<input value={selected.division} onChange={event => updateTeam(team => ({ ...team, division: event.target.value }))} /></label>
                        </div>

                        <div className="cms-team-preview">
                            {selected.logo ? <img src={selected.logo} alt={`${selected.name || "Team"} logo`} /> : <LuImagePlus />}
                            <div><h3>{selected.name || "Team name"}</h3><p>{selected.game || "Game"} · {selectedLeague?.name || "League"} · {selected.division || "Division"}</p></div>
                        </div>

                        <section className="cms-roster-editor">
                            <header><h3>Roster</h3><button type="button" className="cms-icon-button" onClick={addPlayer} title="Add player" aria-label="Add player"><LuUserPlus /></button></header>
                            {selected.players.length === 0 ? <p>No players yet.</p> : selected.players.map((player, index) => <div className="cms-player-row" key={index}>
                                <input value={player.name} onChange={event => updatePlayer(index, "name", event.target.value)} placeholder="Player name" aria-label="Player name" />
                                <input value={player.role} onChange={event => updatePlayer(index, "role", event.target.value)} placeholder="Role" aria-label="Player role" />
                                <input value={player.picture} onChange={event => updatePlayer(index, "picture", event.target.value)} placeholder="/media/teams/player.png" aria-label="Player image URL" />
                                <button type="button" className="cms-icon-button-danger" onClick={() => removePlayer(index)} title="Remove player" aria-label={`Remove ${player.name || "player"}`}><LuX /></button>
                            </div>)}
                        </section>
                    </>}
                </section>
            </div>
        </div>
    );
}
