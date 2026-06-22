"use client";

import { useCallback, useEffect, useRef, useState, type ChangeEvent } from "react";
import { LuChevronRight, LuCopy, LuFolder, LuFolderPlus, LuImage, LuTrash2, LuUpload, LuX } from "react-icons/lu";
import { useCmsToast } from "./CmsToastProvider";

type MediaFile = {
    name: string;
    path: string;
    url: string;
    size: number;
};

type MediaFolder = {
    name: string;
    path: string;
};

type MediaDirectory = {
    folder: string;
    folders: MediaFolder[];
    files: MediaFile[];
};

const emptyDirectory: MediaDirectory = { folder: "", folders: [], files: [] };

export default function CmsMediaLibrary() {
    const [directory, setDirectory] = useState<MediaDirectory>(emptyDirectory);
    const [isLoading, setIsLoading] = useState(true);
    const { showToast } = useCmsToast();
    const [draggingFilePath, setDraggingFilePath] = useState<string | null>(null);
    const [dropTargetFolder, setDropTargetFolder] = useState<string | null>(null);
    const [previewFile, setPreviewFile] = useState<MediaFile | null>(null);
    const [isFileDropActive, setIsFileDropActive] = useState(false);
    const fileDragDepth = useRef(0);

    const loadDirectory = useCallback(async (folder: string) => {
        setIsLoading(true);
        const response = await fetch(`/api/cms/media?folder=${encodeURIComponent(folder)}`);
        const result = await response.json() as MediaDirectory | { message?: string };
        setIsLoading(false);

        if (!response.ok) {
            showToast("message" in result ? result.message ?? "Could not load media." : "Could not load media.", "error");
            return;
        }

        setDirectory(result as MediaDirectory);
    }, [showToast]);

    useEffect(() => {
        void loadDirectory("");
    }, [loadDirectory]);

    async function uploadFiles(event: ChangeEvent<HTMLInputElement>) {
        const files = Array.from(event.target.files ?? []);
        event.target.value = "";
        await uploadImages(files);
    }

    async function uploadImages(files: File[]) {
        if (files.length === 0) return;

        for (const file of files) {
            const formData = new FormData();
            formData.append("operation", "upload");
            formData.append("folder", directory.folder);
            formData.append("file", file);

            const response = await fetch("/api/cms/media", { method: "POST", body: formData });
            if (!response.ok) {
                const result = await response.json() as { message?: string };
                showToast(result.message ?? `Could not upload '${file.name}'.`, "error");
                return;
            }
        }

        showToast(`${files.length} image${files.length === 1 ? "" : "s"} uploaded.`);
        await loadDirectory(directory.folder);
    }

    function handleFileDragEnter(event: React.DragEvent<HTMLDivElement>) {
        if (!containsFiles(event)) return;
        event.preventDefault();
        fileDragDepth.current += 1;
        setIsFileDropActive(true);
    }

    function handleFileDragOver(event: React.DragEvent<HTMLDivElement>) {
        if (!containsFiles(event)) return;
        event.preventDefault();
        event.dataTransfer.dropEffect = "copy";
    }

    function handleFileDragLeave(event: React.DragEvent<HTMLDivElement>) {
        if (!containsFiles(event)) return;
        fileDragDepth.current = Math.max(0, fileDragDepth.current - 1);
        if (fileDragDepth.current === 0)
            setIsFileDropActive(false);
    }

    function handleFileDrop(event: React.DragEvent<HTMLDivElement>) {
        if (!containsFiles(event)) return;
        event.preventDefault();
        fileDragDepth.current = 0;
        setIsFileDropActive(false);
        void uploadImages(Array.from(event.dataTransfer.files));
    }

    async function createFolder() {
        const name = window.prompt("Folder name:");
        if (name === null || !name.trim()) return;

        const formData = new FormData();
        formData.append("operation", "create-folder");
        formData.append("folder", directory.folder);
        formData.append("name", name);
        const response = await fetch("/api/cms/media", { method: "POST", body: formData });
        if (!response.ok) {
            const result = await response.json() as { message?: string };
            showToast(result.message ?? "Could not create folder.", "error");
            return;
        }

        await loadDirectory(directory.folder);
    }

    async function deleteFile(file: MediaFile) {
        if (!window.confirm(`Delete '${file.name}'?`)) return;

        const response = await fetch(`/api/cms/media?path=${encodeURIComponent(file.path)}`, { method: "DELETE" });
        if (!response.ok) {
            const result = await response.json() as { message?: string };
            showToast(result.message ?? "Could not delete image.", "error");
            return;
        }

        await loadDirectory(directory.folder);
    }

    async function deleteFolder(folder: MediaFolder) {
        if (!window.confirm(`Delete the empty folder '${folder.name}'?`)) return;

        const response = await fetch(`/api/cms/media?kind=folder&path=${encodeURIComponent(folder.path)}`, { method: "DELETE" });
        if (!response.ok) {
            const result = await response.json() as { message?: string };
            showToast(result.message ?? "Could not delete folder.", "error");
            return;
        }

        await loadDirectory(directory.folder);
    }

    async function copyUrl(file: MediaFile) {
        await navigator.clipboard.writeText(file.url);
        showToast(`Copied ${file.url}`, "info");
    }

    function startDraggingFile(event: React.DragEvent<HTMLElement>, file: MediaFile) {
        event.dataTransfer.effectAllowed = "move";
        event.dataTransfer.setData("text/plain", file.path);
        setDraggingFilePath(file.path);
    }

    async function dropFile(event: React.DragEvent<HTMLElement>, folder: string) {
        event.preventDefault();
        const filePath = event.dataTransfer.getData("text/plain") || draggingFilePath;
        setDraggingFilePath(null);
        setDropTargetFolder(null);
        if (!filePath) return;

        const response = await fetch("/api/cms/media", {
            method: "PUT",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ path: filePath, folder }),
        });
        if (!response.ok) {
            const result = await response.json() as { message?: string };
            showToast(result.message ?? "Could not move image.", "error");
            return;
        }

        showToast("Image moved.", "info");
        await loadDirectory(directory.folder);
    }

    const breadcrumbParts = directory.folder ? directory.folder.split("/") : [];
    const parentFolder = breadcrumbParts.slice(0, -1).join("/");

    return (
        <div className="cms-media-library" onDragEnter={handleFileDragEnter} onDragOver={handleFileDragOver} onDragLeave={handleFileDragLeave} onDrop={handleFileDrop}>
            <header className="cms-workspace-header cms-media-header">
                <div>
                    <p className="cms-eyebrow">Media</p>
                    <h2>File management</h2>
                </div>
                <div className="cms-media-actions">
                    <button type="button" onClick={() => void createFolder()} title="New folder"><LuFolderPlus /> New folder</button>
                    <label className="cms-media-upload"><LuUpload /> Upload images<input type="file" accept="image/jpeg,image/png,image/webp,image/avif,image/gif" multiple onChange={event => void uploadFiles(event)} /></label>
                </div>
            </header>

            <div className="cms-media-breadcrumbs">
                <button type="button" className={dropTargetFolder === "" ? "drop-target" : ""} onClick={() => void loadDirectory("")} onDragOver={event => { event.preventDefault(); setDropTargetFolder(""); }} onDragLeave={() => setDropTargetFolder(current => current === "" ? null : current)} onDrop={event => void dropFile(event, "")}>Media</button>
                {breadcrumbParts.map((part, index) => {
                    const folder = breadcrumbParts.slice(0, index + 1).join("/");
                    return <span key={folder}><LuChevronRight /><button type="button" onClick={() => void loadDirectory(folder)}>{part}</button></span>;
                })}
            </div>

            <div className="cms-media-browser">
                <aside className="cms-media-folders">
                    {directory.folder && <button type="button" className={dropTargetFolder === parentFolder ? "drop-target" : ""} onClick={() => void loadDirectory(parentFolder)} onDragOver={event => { event.preventDefault(); setDropTargetFolder(parentFolder); }} onDragLeave={() => setDropTargetFolder(current => current === parentFolder ? null : current)} onDrop={event => void dropFile(event, parentFolder)}><LuFolder /> ..</button>}
                    {directory.folders.map(folder => <div key={folder.path} className="cms-media-folder-row"><button type="button" className={dropTargetFolder === folder.path ? "drop-target" : ""} onClick={() => void loadDirectory(folder.path)} onDragOver={event => { event.preventDefault(); setDropTargetFolder(folder.path); }} onDragLeave={() => setDropTargetFolder(current => current === folder.path ? null : current)} onDrop={event => void dropFile(event, folder.path)}><LuFolder /> {folder.name}</button><button className="cms-media-folder-delete" type="button" onClick={() => void deleteFolder(folder)} title={`Delete ${folder.name}`} aria-label={`Delete ${folder.name}`}><LuTrash2 /></button></div>)}
                </aside>
                <section className="cms-media-files" aria-busy={isLoading}>
                    {isLoading ? <p>Loading media…</p> : directory.files.length === 0 ? <p className="cms-media-empty"><LuImage /> This folder has no images.</p> : directory.files.map(file => (
                        <article key={file.path} className="cms-media-file-card" draggable onDragStart={event => startDraggingFile(event, file)} onDragEnd={() => { setDraggingFilePath(null); setDropTargetFolder(null); }}>
                            <button className="cms-media-preview-button" type="button" onClick={() => setPreviewFile(file)} title={`Preview ${file.name}`}><img src={file.url} alt="" /></button>
                            <div>
                                <strong title={file.name}>{file.name}</strong>
                                <small>{formatFileSize(file.size)}</small>
                            </div>
                            <div className="cms-media-file-actions">
                                <button type="button" onClick={() => void copyUrl(file)} title="Copy image URL" aria-label={`Copy URL for ${file.name}`}><LuCopy /></button>
                                <button type="button" onClick={() => void deleteFile(file)} title="Delete image" aria-label={`Delete ${file.name}`}><LuTrash2 /></button>
                            </div>
                        </article>
                    ))}
                </section>
            </div>
            {previewFile && <div className="cms-media-lightbox" role="dialog" aria-modal="true" aria-label={`Preview ${previewFile.name}`} onClick={() => setPreviewFile(null)}>
                <button className="cms-media-lightbox-close" type="button" onClick={() => setPreviewFile(null)} aria-label="Close preview"><LuX /></button>
                <img src={previewFile.url} alt={previewFile.name} onClick={event => event.stopPropagation()} />
                <p>{previewFile.name}</p>
            </div>}
            {isFileDropActive && <div className="cms-media-file-drop-overlay" aria-hidden="true"><LuUpload /><strong>Drop images to upload</strong><span>They will be added to {directory.folder ? `/${directory.folder}` : "the Media folder"}.</span></div>}
        </div>
    );
}

function containsFiles(event: React.DragEvent<HTMLElement>): boolean {
    return Array.from(event.dataTransfer.types).includes("Files");
}

function formatFileSize(bytes: number): string {
    if (bytes < 1024 * 1024) return `${Math.max(1, Math.round(bytes / 1024))} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}
