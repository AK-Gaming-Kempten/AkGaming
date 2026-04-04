(() => {
  const STORAGE_KEY = "theme";
  const LEGACY_STORAGE_KEY = "akg.theme.preference";

  function applyTheme(value) {
    if (value === "light" || value === "dark") {
      document.documentElement.setAttribute("data-theme", value);
    } else {
      document.documentElement.removeAttribute("data-theme");
    }
  }

  function getStoredTheme() {
    return localStorage.getItem(STORAGE_KEY) || localStorage.getItem(LEGACY_STORAGE_KEY) || "system";
  }

  function setStoredTheme(value) {
    localStorage.setItem(STORAGE_KEY, value);
    localStorage.removeItem(LEGACY_STORAGE_KEY);
  }

  function syncPickers(value) {
    document.querySelectorAll("[data-theme-picker]").forEach((el) => {
      if (el instanceof HTMLSelectElement) {
        el.value = value;
      }
    });
  }

  function initialize() {
    const initial = getStoredTheme();
    syncPickers(initial);
    document.querySelectorAll("[data-theme-picker]").forEach((el) => {
      el.addEventListener("change", (event) => {
        const target = event.target;
        if (!(target instanceof HTMLSelectElement)) return;

        const value = target.value === "light" || target.value === "dark" ? target.value : "system";
        setStoredTheme(value);
        applyTheme(value);
        syncPickers(value);
      });
    });
  }

  // Apply theme as early as possible.
  applyTheme(getStoredTheme());
  document.addEventListener("DOMContentLoaded", initialize);
})();
