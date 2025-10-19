import { useEffect, useState } from "react";
import "./Esports.css";

import { loadGames } from "../data/loadGames";
import { loadTeams } from "../data/loadTeams";
import type { EsportsGame, EsportsTeam } from "../data/types";
import EsportsGameSection from "../components/esports/EsportsGameSection";

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
            <p className="info-section">
                <h1>Esports</h1>
                <p>
                    <b>E-Sports</b> bezeichnet den sportlichen Wettkampf zwischen mehreren Menschen, der in einem ausgewählten Computerspiel ausgetragen wird. Dabei kommt es besonders auf <b>Teamfähigkeit, Anpassungsfähigkeit, Koordinationsfähigkeit, strategisches Denken und Kommunikationsfähigkeit</b> unter den Teilnehmern an.
                </p>

                <p>
                    Bei uns gibt es Teams für die <b>meisten bekannten E-Sports-Titel</b> wie League of Legends, CS2, Overwatch und mehr. Unser Roster ist einer gewissen <b>Fluktuation</b> asgesetzt, weshalb wir teils zeitweise mehrere Teams pro Spiel oder kein Team für manche Spiele aufstellen. Wir sind immer offen gegenüber <b>Vorschlägen</b> für neue Disziplinen und Kooperationen.
                </p>

                <p>
                    Unsere Teams treten in <b>verschiedenen Ligen und Turnieren</b> an. Die meisten Teams sind teil der <b><a href="https://www.uniliga.gg/" >Uniliga</a></b>, die speziell für Studentische Teams eine kompetitive Umgebung bietet. Wer teil eines unserer Teams werden möchhte kann sich gern auf unserem <a href="https://discord.gg/akgaming">Discord</a> melden.
                </p>
            </p>


            <p className="evb-section">
                <h1>EVB</h1>
                <p>
                    Wird sind stolzes Gründungsmitglied des <b>E-Sport Verband Bayern</b>. Der EVB trägt dazu bei, den E-Sport in der Region Bayern zu <b>professionalisieren</b> und die Bedeutung von E-Sports in der Gesellschaft zu stärken und die Entwicklung von Talennten zu fördern.
                </p>
                <a href="https://esport.bayern/" target="_blank" rel="noopener noreferrer" >
                    <img src="public/assets/EVB_LogoBlue.png" alt="EVB Logo" className="evb-logo"/>
                </a>
            </p>

            <h1>Unsere Teams</h1>
            {games.map((game) => {
                const gameTeams = teams.filter(
                    (t) => t.game.toLowerCase() === game.id.toLowerCase()
                );

                return (
                    <EsportsGameSection
                        gameName={game.displayName}
                        gameLogo={game.logo}
                        teams={gameTeams}
                    />
                );
            })}
        </main>
    );
}
