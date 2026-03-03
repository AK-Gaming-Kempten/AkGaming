import { useState } from "react";
import { NavLink } from "react-router-dom";
import { FaBars, FaTimes } from "react-icons/fa";
import "./Navbar.css";

export default function Navbar() {
    const [menuOpen, setMenuOpen] = useState(false);

    const toggleMenu = () => setMenuOpen(!menuOpen);
    const closeMenu = () => setMenuOpen(false);

    return (
        <nav className="navbar">
            <div className="container navbar-content">
                <button
                    className="burger"
                    onClick={toggleMenu}
                    aria-label={menuOpen ? "Close navigation menu" : "Open navigation menu"}
                >
                    {menuOpen ? (
                        <FaTimes className="burger-icon" />
                    ) : (
                        <FaBars className="burger-icon" />
                    )}
                </button>

                <ul className={`nav-links ${menuOpen ? "active" : ""}`}>
                    <li><NavLink to="/" end onClick={closeMenu}>Home</NavLink></li>
                    <li><NavLink to="/events" onClick={closeMenu}>Events</NavLink></li>
                    <li><NavLink to="/esports" onClick={closeMenu}>Esports</NavLink></li>
                    <li><NavLink to="/mitgliedschaft" onClick={closeMenu}>Mitgliedschaft</NavLink></li>
                    <li><NavLink to="/impressum" onClick={closeMenu}>Impressum</NavLink></li>
                </ul>
            </div>
        </nav>
    );
}
