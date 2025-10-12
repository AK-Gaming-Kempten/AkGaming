import { Link } from "react-router-dom";

export default function Navbar() {
    return (
        <nav style={{ padding: "1rem", borderBottom: "1px solid #ccc" }}>
            <ul style={{ display: "flex", gap: "1rem", listStyle: "none", margin: 0 }}>
                <li><Link to="/">Home</Link></li>
                <li><Link to="/events">Events</Link></li>
                <li><Link to="/esports">Esports</Link></li>
                <li><Link to="/impressum">Impressum</Link></li>
            </ul>
        </nav>
    );
}