import "./EsportsTeam.css";
import EsportsPlayerCard from "./EsportsPlayerCard";

export interface Player {
    name: string;
    role: string;
    picture: string;
}

export interface League {
    name: string;
    logo: string;
    division: string;
}

export interface Team {
    name: string;
    logo: string;
    league: League;
    players: Player[];
}

export default function EsportsTeam({ name, logo, league, players }: Team) {
    return (
        <div className="team-slide">
            {/* Left column: team info */}
            <div className="team-info">
                <img src={logo} alt={`${name} logo`} className="team-logo" />
                <h3 className="team-name">{name}</h3>

                <div className="league-info">
                    <img src={league.logo} alt={`${league.name} logo`} className="league-logo" />
                    <div className="league-text">
                        <p className="league-name">{league.name}</p>
                        <p className="league-division">{league.division}</p>
                    </div>
                </div>
            </div>

            {/* Right: player list */}
            <div className="players-row">
                {players.map((player) => (
                    <EsportsPlayerCard key={player.name} {...player} />
                ))}
            </div>
        </div>
    );
}