import EsportsGameSection from "../components/esports/EsportsGameSection";
import lolLogo from "../assets/games/esports_lol.svg";
import csLogo from "../assets/games/esports_cs2.webp";
import valorantLogo from "../assets/games/esports_valorant.png";
import ow2Logo from "../assets/games/esports_ow2.png";
import r6sLogo from "../assets/games/esports_r6s.webp";
import wildRiftLogo from "../assets/games/esports_wildRift.svg";
import twoxkoLogo from "../assets/games/esports_2xko.png";
import tftLogo from "../assets/games/esports_tft.svg";
import lorLogo from "../assets/games/esports_lor.png";
import dragonLogo from "../assets/teams/HSK_Dragon_Logo.png";
import phoenixLogo from "../assets/teams/HSK_Phoenix_Logo.png";
import centauriLogo from "../assets/teams/HSK_Centauri_Logo.png";
import eagleLogo from "../assets/teams/HSK_Eagle_Logo.png";
import demonLogo from "../assets/teams/HSK_Demon_Logo.png";
import uniligaLogo from "../assets/uniliga.png";
import dachcsLogo from "../assets/dachcs.svg";
import defaultLogo from "../assets/akgaming_logo.png";



export default function Esports() {
    const hsk_dragon = {
        name: "HSK Dragon",
        logo: dragonLogo,
        league: {
            name: "Uniliga",
            logo: uniligaLogo,
            division: "Div 3"
        },
        players: [
            { name: "HSK Dragon", role: "HSK Dragon", picture: dragonLogo },
            { name: "HSK Dragon", role: "HSK Dragon", picture: dragonLogo },
            { name: "HSK Dragon", role: "HSK Dragon", picture: dragonLogo },
            { name: "HSK Dragon", role: "HSK Dragon", picture: dragonLogo },
            { name: "HSK Dragon", role: "HSK Dragon", picture: dragonLogo },
        ]
    };
    const hsk_phoenix = {
        name: "HSK Phoenix",
        logo: phoenixLogo,
        league: {
            name: "Uniliga",
            logo: uniligaLogo,
            division: "Div 2"
        },
        players: [
            { name: "HSK Phoenix", role: "HSK Phoenix", picture: phoenixLogo },
            { name: "HSK Phoenix", role: "HSK Phoenix", picture: phoenixLogo },
            { name: "HSK Phoenix", role: "HSK Phoenix", picture: phoenixLogo },
            { name: "HSK Phoenix", role: "HSK Phoenix", picture: phoenixLogo },
            { name: "HSK Phoenix", role: "HSK Phoenix", picture: phoenixLogo },
        ]
    };
    const hsk_centauri = {
        name: "HSK Centauri",
        logo: centauriLogo,
        league: {
            name: "Uniliga",
            logo: uniligaLogo,
            division: "Div 2"
        },
        players: [
            { name: "HSK Centauri", role: "HSK Centauri", picture: centauriLogo },
            { name: "HSK Centauri", role: "HSK Centauri", picture: centauriLogo },
            { name: "HSK Centauri", role: "HSK Centauri", picture: centauriLogo },
            { name: "HSK Centauri", role: "HSK Centauri", picture: centauriLogo },
            { name: "HSK Centauri", role: "HSK Centauri", picture: centauriLogo },
        ]
    };
    const hsk_satyrus = {
        name: "HSK Satyrus",
        logo: defaultLogo,
        league: {
            name: "Uniliga",
            logo: uniligaLogo,
            division: "Div 3"
        },
        players: [
            { name: "HSK Satyrus", role: "HSK Satyrus", picture: defaultLogo },
            { name: "HSK Satyrus", role: "HSK Satyrus", picture: defaultLogo },
            { name: "HSK Satyrus", role: "HSK Satyrus", picture: defaultLogo },
            { name: "HSK Satyrus", role: "HSK Satyrus", picture: defaultLogo },
            { name: "HSK Satyrus", role: "HSK Satyrus", picture: defaultLogo },
        ]
    };
    const hsk_eagle = {
        name: "HSK Eagle",
        logo: eagleLogo,
        league: {
            name: "Uniliga",
            logo: uniligaLogo,
            division: "Div 3"
        },
        players: [
            { name: "HSK Eagle", role: "HSK Eagle", picture: eagleLogo },
            { name: "HSK Eagle", role: "HSK Eagle", picture: eagleLogo },
            { name: "HSK Eagle", role: "HSK Eagle", picture: eagleLogo },
            { name: "HSK Eagle", role: "HSK Eagle", picture: eagleLogo },
            { name: "HSK Eagle", role: "HSK Eagle", picture: eagleLogo },
        ]
    };
    const akg_zamrichten = {
        name: "AKG Zamrichten",
        logo: defaultLogo,
        league: {
            name: "DACH CS",
            logo: dachcsLogo,
            division: "Div 3"
        },
        players: [
            { name: "AKG Zamrichten", role: "AKG Zamrichten", picture: defaultLogo },
            { name: "AKG Zamrichten", role: "AKG Zamrichten", picture: defaultLogo },
            { name: "AKG Zamrichten", role: "AKG Zamrichten", picture: defaultLogo },
            { name: "AKG Zamrichten", role: "AKG Zamrichten", picture: defaultLogo },
            { name: "AKG Zamrichten", role: "AKG Zamrichten", picture: defaultLogo },
        ]
    };
    const hsk_demon = {
        name: "HSK Demon",
        logo: demonLogo,
        league: {
            name: "Uniliga",
            logo: uniligaLogo,
            division: "Div 3"
        },
        players: [
            { name: "HSK Demon", role: "HSK Demon", picture: demonLogo },
            { name: "HSK Demon", role: "HSK Demon", picture: demonLogo },
            { name: "HSK Demon", role: "HSK Demon", picture: demonLogo },
            { name: "HSK Demon", role: "HSK Demon", picture: demonLogo },
            { name: "HSK Demon", role: "HSK Demon", picture: demonLogo },
        ]
    };

    return (
        <main>
            <h1>Esports Teams</h1>

            <EsportsGameSection
                gameName="League of Legends"
                gameLogo={lolLogo}
                teams={[hsk_phoenix, hsk_dragon]}
            />

            <EsportsGameSection
                gameName="CS 2"
                gameLogo={csLogo}
                teams={[hsk_eagle, akg_zamrichten]}
            />

            <EsportsGameSection
                gameName="Overwatch 2"
                gameLogo={ow2Logo}
                teams={[hsk_centauri, hsk_satyrus]}
            />

            <EsportsGameSection
                gameName="Valorant"
                gameLogo={valorantLogo}
                teams={[hsk_demon]}
            />

            <EsportsGameSection
                gameName="Rainbox Six: Siege"
                gameLogo={r6sLogo}
                teams={[]}
            />

            <EsportsGameSection
                gameName="Wild Rift"
                gameLogo={wildRiftLogo}
                teams={[]}
            />

            <EsportsGameSection
                gameName="2XKO"
                gameLogo={twoxkoLogo}
                teams={[]}
            />

            <EsportsGameSection
                gameName="TFT"
                gameLogo={tftLogo}
                teams={[]}
            />

            <EsportsGameSection
                gameName="LoR"
                gameLogo={lorLogo}
                teams={[]}
            />
        </main>
    );
}
