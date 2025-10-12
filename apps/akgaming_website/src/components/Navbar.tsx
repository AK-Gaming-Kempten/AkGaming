import { useState } from "react";
import { NavLink } from "react-router-dom";
import "./Navbar.css";

export default function Navbar() {
    const [menuOpen, setMenuOpen] = useState(false);

    const toggleMenu = () => setMenuOpen(!menuOpen);
    const closeMenu = () => setMenuOpen(false);

    return (
        <nav className="navbar">
            <div className="container navbar-content">
                <button
                    className={`burger ${menuOpen ? "open" : ""}`}
                    onClick={toggleMenu}
                    aria-label="Toggle navigation"
                >
                    <span></span>
                    <span></span>
                    <span></span>
                </button>

                <ul className={`nav-links ${menuOpen ? "active" : ""}`}>
                    <li><NavLink to="/" end onClick={closeMenu}>Home</NavLink></li>
                    <li><NavLink to="/events" onClick={closeMenu}>Events</NavLink></li>
                    <li><NavLink to="/esports" onClick={closeMenu}>Esports</NavLink></li>
                    <li><NavLink to="/impressum" onClick={closeMenu}>Impressum</NavLink></li>
                </ul>
            </div>
        </nav>
    );
}

