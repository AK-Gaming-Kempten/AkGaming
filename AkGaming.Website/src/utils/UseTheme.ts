import { useEffect, useState } from "react";

type Theme = "light" | "dark" | "system";

export function useTheme() {
    const [theme, setTheme] = useState<Theme>("system");

    // Apply theme to document root
    const applyTheme = (t: Theme) => {
        if (t === "system") {
            document.documentElement.removeAttribute("data-theme");
        } else {
            document.documentElement.setAttribute("data-theme", t);
        }
    };

    useEffect(() => {
        // Load stored theme
        const saved = localStorage.getItem("theme") as Theme | null;
        if (saved) {
            setTheme(saved);
            applyTheme(saved);
        }
    }, []);

    const toggleTheme = () => {
        let newTheme: Theme;
        if (theme === "light") newTheme = "dark";
        else if (theme === "dark") newTheme = "system";
        else newTheme = "light"; // system → light

        setTheme(newTheme);
        applyTheme(newTheme);
        localStorage.setItem("theme", newTheme);
    };

    return { theme, toggleTheme };
}
