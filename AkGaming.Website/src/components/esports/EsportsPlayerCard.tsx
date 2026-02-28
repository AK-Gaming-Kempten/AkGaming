import "./EsportsPlayerCard.css";

interface PlayerCardProps {
    name: string;
    role: string;
    picture: string;
}

export default function EsportsPlayerCard({ name, role, picture }: PlayerCardProps) {
    return (
        <div className="player-card">
            <img src={picture} alt={`${name} portrait`} className="player-pic" />
            <p className="player-name">{name}</p>
            <p className="player-role">{role}</p>
        </div>
    );
}
