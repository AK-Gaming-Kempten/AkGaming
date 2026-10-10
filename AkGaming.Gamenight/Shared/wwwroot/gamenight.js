window.gamenight = {
    scrollToSignup: element => {
        element.scrollIntoView({ behavior: "instant", block: "start" });
        element.focus({ preventScroll: true });
    },
    download: (name, text) => {
        const url = URL.createObjectURL(new Blob([text], { type: "text/csv;charset=utf-8" }));
        const link = document.createElement("a");
        link.href = url; link.download = name; link.click();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    }
};


// Anchor the native popover to its trigger, keeping it inside the viewport.
(() => {
    function positionDeadlineInfo() {
        const popover = document.getElementById("event-deadline-info");
        const trigger = document.querySelector('[popovertarget="event-deadline-info"]');
        if (!popover || !trigger || !popover.matches(":popover-open")) return;
        const rect = trigger.getBoundingClientRect();
        const gap = 8;
        const left = Math.max(gap, Math.min(rect.left, window.innerWidth - popover.offsetWidth - gap));
        const below = rect.bottom + gap;
        const top = below + popover.offsetHeight <= window.innerHeight - gap
            ? below : Math.max(gap, rect.top - popover.offsetHeight - gap);
        popover.style.left = `${left}px`;
        popover.style.top = `${top}px`;
    }
    document.addEventListener("toggle", event => {
        if (event.target.id === "event-deadline-info") positionDeadlineInfo();
    }, true);
    window.addEventListener("resize", positionDeadlineInfo);
    document.addEventListener("scroll", positionDeadlineInfo, true);
})();
