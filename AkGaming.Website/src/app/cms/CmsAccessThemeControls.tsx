"use client";

import { LuMonitor, LuMoonStar, LuSunMedium } from "react-icons/lu";
import { useTheme } from "../../utils/UseTheme";

export default function CmsAccessThemeControls() {
    const { theme, setTheme } = useTheme();

    return (
        <div className="cms-access-theme-controls" aria-label="Theme">
            <button type="button" className={theme === "system" ? "active" : ""} onClick={() => setTheme("system")} title="Use system theme" aria-label="Use system theme"><LuMonitor /></button>
            <button type="button" className={theme === "light" ? "active" : ""} onClick={() => setTheme("light")} title="Use light theme" aria-label="Use light theme"><LuSunMedium /></button>
            <button type="button" className={theme === "dark" ? "active" : ""} onClick={() => setTheme("dark")} title="Use dark theme" aria-label="Use dark theme"><LuMoonStar /></button>
        </div>
    );
}
