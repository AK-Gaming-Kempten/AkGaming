"use client";

import { useState } from "react";
import { FaBars, FaTimes } from "react-icons/fa";
import ActiveLink from "./navigation/ActiveLink";
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
                    <li><ActiveLink href="/" exact onClick={closeMenu}>Home</ActiveLink></li>
                    <li><ActiveLink href="/events" onClick={closeMenu}>Events</ActiveLink></li>
                    <li><ActiveLink href="/esports" onClick={closeMenu}>Esports</ActiveLink></li>
                    <li><ActiveLink href="/mitgliedschaft" onClick={closeMenu}>Mitgliedschaft</ActiveLink></li>
                    <li><ActiveLink href="/impressum" onClick={closeMenu}>Impressum</ActiveLink></li>
                </ul>
            </div>
        </nav>
    );
}
