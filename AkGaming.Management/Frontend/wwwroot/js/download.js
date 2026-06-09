window.akGaming = window.akGaming || {};
window.akGaming.downloadFile = (fileName, contentType, base64Content) => {
    const link = document.createElement("a");
    link.href = `data:${contentType};base64,${base64Content}`;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
};
