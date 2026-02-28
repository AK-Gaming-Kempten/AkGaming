(function() {
    const STORAGE_KEY = "theme"; // 'light' | 'dark' | 'system'
    const mql = window.matchMedia("(prefers-color-scheme: dark)");

    function apply(theme) {
        if (theme === "light") {
            document.documentElement.setAttribute("data-theme", "light");
        } else if (theme === "dark") {
            document.documentElement.setAttribute("data-theme", "dark");
        } else {
            // 'system' → remove override; your @media(prefers-color-scheme) takes over
            document.documentElement.removeAttribute("data-theme");
        }
    }

    window.themeApi = {
        init() {
            const saved = localStorage.getItem(STORAGE_KEY) ?? "system";
            apply(saved);
            return saved;
        },
        set(theme) {
            localStorage.setItem(STORAGE_KEY, theme);
            apply(theme);
        },
        current() {
            return localStorage.getItem(STORAGE_KEY) ?? "system";
        },
        // optional: let .NET subscribe to system changes when user chose 'system'
        onSystemChanged(dotnetRef) {
            if (!dotnetRef) return;
            mql.addEventListener("change", () => {
                if ((localStorage.getItem(STORAGE_KEY) ?? "system") === "system") {
                    apply("system"); // re-apply to reflect new system setting
                    dotnetRef.invokeMethodAsync("OnSystemThemeChanged");
                }
            });
        }
    };
})();
