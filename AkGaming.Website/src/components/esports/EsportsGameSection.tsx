import "./EsportsGameSection.css";
import EsportsTeam from "./EsportsTeam";
import type { Team } from "./EsportsTeam";

interface GameSectionProps {
    gameName: string;
    gameLogo: string;
    teams: Team[];
}

export default function GameSection({ gameName, gameLogo, teams }: GameSectionProps) {
    if (teams.length === 0) return null;
    return (
        <section className="game-section">
            <div className="game-header">
                <div className="game-header-left">
                    <img src={gameLogo} alt={`${gameName} logo`} className="game-logo" />
                    <h2 className="game-title">{gameName}</h2>
                </div>
                <div className="game-header-line"></div>
            </div>

            <div className="teams-container">
                {teams.map((team) => (
                    <EsportsTeam key={team.name} {...team} />
                ))}
            </div>
        </section>
    );
}
