import "./SponsorCard.css";
import sponsorLogo from "../../../public/assets/soloplan_mit_text.png";

export default function SponsorCard() {
    return (
        <div className="sponsor-card">
            <h3>Unser Hauptsponsor</h3>
            <a href="https://soloplan.de" target="_blank" rel="noopener noreferrer" >
                <img src={sponsorLogo} alt="Soloplan logo" className="sponsor-logo"/>
            </a>
        </div>
    );
}
