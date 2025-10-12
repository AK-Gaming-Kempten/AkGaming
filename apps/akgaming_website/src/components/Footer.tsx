import { Link } from "react-router-dom";

export default function Footer() {
    return (
        <footer style={{
            marginTop: "2rem",
            padding: "1rem",
            borderTop: "1px solid #ccc",
            textAlign: "center"
        }}>
            <p>© {new Date().getFullYear()} My Website — <Link to="/impressum">Impressum</Link></p>
        </footer>
    );
}