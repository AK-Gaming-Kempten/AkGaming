document.addEventListener("dragstart", event => {
    const source = event.target instanceof Element
        ? event.target.closest("[data-ak-draggable='true']")
        : null;

    if (!source || !event.dataTransfer) {
        return;
    }

    event.dataTransfer.effectAllowed = "move";
    event.dataTransfer.setData("text/plain", source.dataset.akDragId ?? "akgaming-drag-item");
}, true);
