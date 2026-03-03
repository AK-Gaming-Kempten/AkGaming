import { useEffect, useState } from "react";
import "./Esports.css";

import { loadGames } from "../data/loadGames";
import { loadTeams } from "../data/loadTeams";
import type { EsportsGame, EsportsTeam } from "../data/types";
import EsportsGameSection from "../components/esports/EsportsGameSection";
import EvbIcon from "../../public/assets/EVB_LogoBlue.png";

export default function Esports() {
    const [games, setGames] = useState<EsportsGame[]>([]);
    const [teams, setTeams] = useState<EsportsTeam[]>([]);

    useEffect(() => {
        Promise.all([loadGames(), loadTeams()]).then(([loadedGames, loadedTeams]) => {
            setGames(loadedGames);
            setTeams(loadedTeams);
        });
    }, []);

    return (
        <main className="esports">
            <section className="esports-hero">
                <p className="esports-eyebrow">AK Gaming e.V.</p>
                <h1>Esports</h1>
                <p className="esports-hero-copy">
                    Wettbewerb, Teamgeist und regionale Talentförderung: Unsere Teams treten
                    in verschiedenen Titeln, Ligen und Turnieren an und entwickeln sich gemeinsam.
                </p>
                <div className="esports-hero-actions">
                    <a href="#esports-teams" className="esports-btn esports-btn-primary">Teams ansehen</a>
                    <a href="https://discord.gg/5J5uJKJAhT" target="_blank" rel="noreferrer" className="esports-btn esports-btn-secondary">
                        Auf Discord bewerben
                    </a>
                </div>
            </section>

            <section className="esports-info-grid">
                <article className="esports-panel">
                    <h2>Unser Ansatz</h2>
                    <p>
                        E-Sports ist für uns strukturierter Wettbewerb in digitalen Disziplinen.
                        Entscheidend sind Teamfähigkeit, Kommunikation, Koordination und
                        strategisches Denken.
                    </p>
                    <p>
                        Wir stellen Teams in bekannten Titeln wie League of Legends, CS2,
                        Overwatch und weiteren Disziplinen. Das Roster entwickelt sich laufend,
                        deshalb sind neue Vorschläge und Kooperationen jederzeit willkommen.
                    </p>
                    <p>
                        Viele Teams spielen in der{" "}
                        <a href="https://www.uniliga.gg/" target="_blank" rel="noreferrer">Uniliga</a>,
                        die speziell für studentische Teams ein kompetitives Umfeld bietet.
                    </p>
                </article>

                <article className="esports-panel esports-panel-evb">
                    <h2>EVB</h2>
                    <p>
                        Wir sind Gründungsmitglied des{" "}
                        <b>E-Sport Verbands Bayern</b> und unterstützen damit den
                        nachhaltigen Aufbau professioneller E-Sports-Strukturen in der Region.
                    </p>
                    <a href="https://esport.bayern/" target="_blank" rel="noopener noreferrer">
                        <img src={EvbIcon} alt="EVB Logo" className="evb-logo" />
                    </a>
                </article>
            </section>

            <section id="esports-teams" className="esports-teams-section">
                <div className="esports-teams-heading">
                    <h2>Unsere Teams</h2>
                    <p>Aktive Lineups nach Disziplin</p>
                </div>

                {games.map((game) => {
                    const gameTeams = teams.filter(
                        (t) => t.game.toLowerCase() === game.id.toLowerCase()
                    );

                    return (
                        <EsportsGameSection
                            key={game.id}
                            gameName={game.displayName}
                            gameLogo={game.logo}
                            teams={gameTeams}
                        />
                    );
                })}
            </section>
        </main>
    );
}
