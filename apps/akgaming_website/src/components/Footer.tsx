import { Link } from "react-router-dom";
import "./Footer.css";

export default function Footer() {
    return (
        <footer className="footer">
            <p>
                © {new Date().getFullYear()} AK Gaming e.V. — <Link to="/impressum">Impressum</Link>
            </p>
        </footer>
    );
}
