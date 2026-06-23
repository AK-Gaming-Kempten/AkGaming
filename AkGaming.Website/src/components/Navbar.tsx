"use client";

import ActiveLink from "./navigation/ActiveLink";
import "./Navbar.css";

type NavbarProps = {
    menuOpen: boolean;
    onCloseMenu: () => void;
};

export default function Navbar({ menuOpen, onCloseMenu }: NavbarProps) {

    return (
        <nav className="navbar">
            <div className="container navbar-content">
                <ul className={`nav-links ${menuOpen ? "active" : ""}`}>
                    <li><ActiveLink href="/" exact onClick={onCloseMenu}>Home</ActiveLink></li>
                    <li><ActiveLink href="/events" onClick={onCloseMenu}>Events</ActiveLink></li>
                    <li><ActiveLink href="/esports" onClick={onCloseMenu}>Esports</ActiveLink></li>
                    <li><ActiveLink href="/mitgliedschaft" onClick={onCloseMenu}>Mitgliedschaft</ActiveLink></li>
                    <li><ActiveLink href="/impressum" onClick={onCloseMenu}>Impressum</ActiveLink></li>
                </ul>
            </div>
        </nav>
    );
}
