"use client";

import { useEffect, useState } from "react";
import Header from "./Header";
import Navbar from "./Navbar";

export default function TopChrome() {
    const [isMenuOpen, setIsMenuOpen] = useState(false);

    useEffect(() => {
        document.body.classList.toggle("mobile-menu-open", isMenuOpen);
        return () => document.body.classList.remove("mobile-menu-open");
    }, [isMenuOpen]);

    return (
        <div className={`top-chrome${isMenuOpen ? " menu-open" : ""}`}>
            <Header menuOpen={isMenuOpen} onToggleMenu={() => setIsMenuOpen(current => !current)} />
            <Navbar menuOpen={isMenuOpen} onCloseMenu={() => setIsMenuOpen(false)} />
        </div>
    );
}
