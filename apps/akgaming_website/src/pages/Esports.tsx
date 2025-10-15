import { useEffect, useState } from "react";
import "./Esports.css";

import { loadGames } from "../data/loadGames";
import { loadTeams } from "../data/loadTeams";
import type { EsportsGame, EsportsTeam } from "../data/types";
import EsportsGameSection from "../components/esports/EsportsGameSection";

export default function Esports() {
    // const hsk_dragon = {
    //     name: "HSK Dragon",
    //     logo: dragonLogo,
    //     league: {
    //         name: "Uniliga",
    //         logo: uniligaLogo,
    //         division: "Div 3"
    //     },
    //     players: [
    //         { name: "HSK Dragon", role: "HSK Dragon", picture: dragonLogo },
    //         { name: "HSK Dragon", role: "HSK Dragon", picture: dragonLogo },
    //         { name: "HSK Dragon", role: "HSK Dragon", picture: dragonLogo },
    //         { name: "HSK Dragon", role: "HSK Dragon", picture: dragonLogo },
    //         { name: "HSK Dragon", role: "HSK Dragon", picture: dragonLogo },
    //     ]
    // };
    // const hsk_phoenix = {
    //     name: "HSK Phoenix",
    //     logo: phoenixLogo,
    //     league: {
    //         name: "Uniliga",
    //         logo: uniligaLogo,
    //         division: "Div 2"
    //     },
    //     players: [
    //         { name: "HSK Phoenix", role: "HSK Phoenix", picture: phoenixLogo },
    //         { name: "HSK Phoenix", role: "HSK Phoenix", picture: phoenixLogo },
    //         { name: "HSK Phoenix", role: "HSK Phoenix", picture: phoenixLogo },
    //         { name: "HSK Phoenix", role: "HSK Phoenix", picture: phoenixLogo },
    //         { name: "HSK Phoenix", role: "HSK Phoenix", picture: phoenixLogo },
    //     ]
    // };
    // const hsk_centauri = {
    //     name: "HSK Centauri",
    //     logo: centauriLogo,
    //     league: {
    //         name: "Uniliga",
    //         logo: uniligaLogo,
    //         division: "Div 2"
    //     },
    //     players: [
    //         { name: "HSK Centauri", role: "HSK Centauri", picture: centauriLogo },
    //         { name: "HSK Centauri", role: "HSK Centauri", picture: centauriLogo },
    //         { name: "HSK Centauri", role: "HSK Centauri", picture: centauriLogo },
    //         { name: "HSK Centauri", role: "HSK Centauri", picture: centauriLogo },
    //         { name: "HSK Centauri", role: "HSK Centauri", picture: centauriLogo },
    //     ]
    // };
    // const hsk_satyrus = {
    //     name: "HSK Satyrus",
    //     logo: defaultLogo,
    //     league: {
    //         name: "Uniliga",
    //         logo: uniligaLogo,
    //         division: "Div 3"
    //     },
    //     players: [
    //         { name: "HSK Satyrus", role: "HSK Satyrus", picture: defaultLogo },
    //         { name: "HSK Satyrus", role: "HSK Satyrus", picture: defaultLogo },
    //         { name: "HSK Satyrus", role: "HSK Satyrus", picture: defaultLogo },
    //         { name: "HSK Satyrus", role: "HSK Satyrus", picture: defaultLogo },
    //         { name: "HSK Satyrus", role: "HSK Satyrus", picture: defaultLogo },
    //     ]
    // };
    // const hsk_eagle = {
    //     name: "HSK Eagle",
    //     logo: eagleLogo,
    //     league: {
    //         name: "Uniliga",
    //         logo: uniligaLogo,
    //         division: "Div 3"
    //     },
    //     players: [
    //         { name: "HSK Eagle", role: "HSK Eagle", picture: eagleLogo },
    //         { name: "HSK Eagle", role: "HSK Eagle", picture: eagleLogo },
    //         { name: "HSK Eagle", role: "HSK Eagle", picture: eagleLogo },
    //         { name: "HSK Eagle", role: "HSK Eagle", picture: eagleLogo },
    //         { name: "HSK Eagle", role: "HSK Eagle", picture: eagleLogo },
    //     ]
    // };
    // const akg_zamrichten = {
    //     name: "AKG Zamrichten",
    //     logo: defaultLogo,
    //     league: {
    //         name: "DACH CS",
    //         logo: dachcsLogo,
    //         division: "Div 3"
    //     },
    //     players: [
    //         { name: "AKG Zamrichten", role: "AKG Zamrichten", picture: defaultLogo },
    //         { name: "AKG Zamrichten", role: "AKG Zamrichten", picture: defaultLogo },
    //         { name: "AKG Zamrichten", role: "AKG Zamrichten", picture: defaultLogo },
    //         { name: "AKG Zamrichten", role: "AKG Zamrichten", picture: defaultLogo },
    //         { name: "AKG Zamrichten", role: "AKG Zamrichten", picture: defaultLogo },
    //     ]
    // };
    // const hsk_demon = {
    //     name: "HSK Demon",
    //     logo: demonLogo,
    //     league: {
    //         name: "Uniliga",
    //         logo: uniligaLogo,
    //         division: "Div 3"
    //     },
    //     players: [
    //         { name: "HSK Demon", role: "HSK Demon", picture: demonLogo },
    //         { name: "HSK Demon", role: "HSK Demon", picture: demonLogo },
    //         { name: "HSK Demon", role: "HSK Demon", picture: demonLogo },
    //         { name: "HSK Demon", role: "HSK Demon", picture: demonLogo },
    //         { name: "HSK Demon", role: "HSK Demon", picture: demonLogo },
    //     ]
    // };

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
            <h1>Esports</h1>
            <p>Infos über unseren Esports-Bereich.</p>

            <h1>EVB</h1>
            <p>Wird sind stolzes Gründungsmitglied des E-Sport Verband Bayern.</p>

            <h1>Esports Teams</h1>
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
