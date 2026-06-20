import Link from "next/link";
import "./Footer.css";

export default function Footer() {
    return (
        <footer className="footer">
            <p>
                © {new Date().getFullYear()} AK Gaming e.V. — <Link href="/impressum">Impressum</Link>
            </p>
        </footer>
    );
}
