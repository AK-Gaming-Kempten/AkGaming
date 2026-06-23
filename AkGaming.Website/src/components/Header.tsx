"use client";

import "./Header.css";
import { useTheme } from "../utils/UseTheme";
import Link from "next/link";
import { LuSunMedium, LuMoonStar, LuMonitor, LuPencil, LuMenu, LuX } from "react-icons/lu";

type HeaderProps = {
    menuOpen: boolean;
    onToggleMenu: () => void;
};

export default function Header({ menuOpen, onToggleMenu }: HeaderProps) {
    const { theme, toggleTheme } = useTheme();

    const getIcon = () => {
        switch (theme) {
            case "light":
                return <LuSunMedium />;
            case "dark":
                return <LuMoonStar />;
            default:
                return <LuMonitor />;
        }
    };

    return (
        <header className="header">
            <Link href="/cms" className="cms-entry-link" title="Open CMS" aria-label="Open CMS"><LuPencil /></Link>
            <button className="mobile-menu-toggle" type="button" onClick={onToggleMenu} title={menuOpen ? "Close navigation menu" : "Open navigation menu"} aria-label={menuOpen ? "Close navigation menu" : "Open navigation menu"}>
                {menuOpen ? <LuX /> : <LuMenu />}
            </button>
            <button className="theme-toggle" onClick={toggleTheme} title={`Theme: ${theme}`}>
                {getIcon()}
            </button>

            <div className="header-content">
                <img src="/assets/akgaming_logo.png" alt="AK Gaming e.V. Logo" className="header-logo" />
                <h1 className="header-title">AK Gaming e.V.</h1>
            </div>
        </header>
    );
}
