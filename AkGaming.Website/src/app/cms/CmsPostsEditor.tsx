"use client";

import { useEffect, useState } from "react";
import {
    LuCalendarDays,
    LuChevronDown,
    LuChevronRight,
    LuFileText,
    LuFolder,
    LuFolderPlus,
    LuMonitor,
    LuMoonStar,
    LuPanelLeftClose,
    LuPanelLeftOpen,
    LuPencil,
    LuPlus,
    LuSave,
    LuSunMedium,
    LuUpload,
    LuTrash2,
} from "react-icons/lu";
import { useTheme } from "../../utils/UseTheme";
import CmsMediaLibrary from "./CmsMediaLibrary";
import CmsHighlightsManager from "./CmsHighlightsManager";
import MdxEditor from "./MdxEditor";

type Section = "posts" | "files" | "highlights";
type EditorTab = "metadata" | "mdx";
type ContentKind = "post" | "event";

type CmsPost = {
    type: ContentKind;
    id: string;
    title: string;
    shortDescription: string;
    body: string;
    startDate?: string;
    endDate?: string;
    location?: string;
    locationUrl?: string;
    folderId?: string;
    isDraft: boolean;
};

type CmsFolder = {
    id: string;
    name: string;
};

type CmsPostsEditorProps = {
    email?: string | null;
    signOutAction: () => Promise<void>;
};

const emptyPost = (): CmsPost => ({
    type: "post",
    id: "",
    title: "",
    shortDescription: "",
    body: "",
    isDraft: true,
});

export default function CmsPostsEditor({ email, signOutAction }: CmsPostsEditorProps) {
    const { theme, setTheme } = useTheme();
    const [section, setSection] = useState<Section>("posts");
    const [tab, setTab] = useState<EditorTab>("metadata");
    const [isPostSelectorExpanded, setIsPostSelectorExpanded] = useState(true);
    const [posts, setPosts] = useState<CmsPost[]>([]);
    const [folders, setFolders] = useState<CmsFolder[]>([]);
    const [selected, setSelected] = useState<CmsPost>(emptyPost());
    const [draggingPostId, setDraggingPostId] = useState<string | null>(null);
    const [dropTargetFolderId, setDropTargetFolderId] = useState<string | null>(null);
    const [collapsedFolderIds, setCollapsedFolderIds] = useState<string[]>([]);
    const [message, setMessage] = useState("");
    const [previewKey, setPreviewKey] = useState(0);

    useEffect(() => {
        void reload();
    }, []);

    async function reload() {
        const [postsResponse, foldersResponse] = await Promise.all([
            fetch("/api/cms/posts"),
            fetch("/api/cms/post-folders"),
        ]);

        if (postsResponse.ok)
            setPosts(await postsResponse.json() as CmsPost[]);
        if (foldersResponse.ok)
            setFolders(await foldersResponse.json() as CmsFolder[]);
    }

    async function save() {
        const response = await fetch("/api/cms/posts", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(selected),
        });
        const result = await response.json() as CmsPost | { message: string };

        if (!response.ok) {
            setMessage("message" in result ? result.message : "Save failed.");
            return;
        }

        setSelected(result as CmsPost);
        setMessage("Draft saved. Preview refreshed.");
        setPreviewKey(key => key + 1);
        await reload();
    }

    async function publish() {
        const response = await fetch(`/api/cms/posts/${encodeURIComponent(selected.id)}/publish`, { method: "POST" });
        const result = await response.json() as CmsPost | { message: string };

        if (!response.ok) {
            setMessage("message" in result ? result.message : "Publish failed.");
            return;
        }

        setSelected(result as CmsPost);
        setMessage("Published.");
        await reload();
    }

    function update(field: keyof CmsPost, value: string) {
        setSelected(current => ({ ...current, [field]: value }));
    }

    async function createFolder() {
        const name = window.prompt("Folder name:");
        if (name === null || !name.trim()) return;

        const response = await fetch("/api/cms/post-folders", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ name }),
        });
        if (!response.ok) {
            const result = await response.json() as { message?: string };
            setMessage(result.message ?? "Folder creation failed.");
            return;
        }

        await reload();
    }

    async function renameFolder(folder: CmsFolder) {
        const name = window.prompt("Folder name:", folder.name);
        if (name === null || !name.trim() || name === folder.name) return;

        const response = await fetch(`/api/cms/post-folders/${encodeURIComponent(folder.id)}`, {
            method: "PATCH",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ name }),
        });
        if (!response.ok) {
            const result = await response.json() as { message?: string };
            setMessage(result.message ?? "Folder rename failed.");
            return;
        }

        await reload();
    }

    async function deleteFolder(folder: CmsFolder) {
        if (!window.confirm(`Delete '${folder.name}'? Posts in this folder will become unsorted.`)) return;

        const response = await fetch(`/api/cms/post-folders/${encodeURIComponent(folder.id)}`, { method: "DELETE" });
        if (!response.ok) {
            const result = await response.json() as { message?: string };
            setMessage(result.message ?? "Folder deletion failed.");
            return;
        }

        await reload();
    }

    async function movePost(postId: string, folderId: string | null) {
        const post = posts.find(candidate => candidate.id === postId);
        if (post === undefined || (post.folderId ?? null) === folderId) return;

        const response = await fetch(`/api/cms/posts/${encodeURIComponent(postId)}/folder`, {
            method: "PUT",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ folderId }),
        });
        const result = await response.json() as CmsPost | { message?: string };
        if (!response.ok) {
            setMessage("message" in result ? result.message ?? "Could not move post." : "Could not move post.");
            return;
        }

        const movedPost = result as CmsPost;
        setPosts(current => current.map(candidate => candidate.id === postId ? movedPost : candidate));
        setSelected(current => current.id === postId ? movedPost : current);
        setMessage(`Moved '${movedPost.title}'.`);
    }

    function startDraggingPost(event: React.DragEvent<HTMLButtonElement>, postId: string) {
        event.dataTransfer.effectAllowed = "move";
        event.dataTransfer.setData("text/plain", postId);
        setDraggingPostId(postId);
    }

    async function dropPost(event: React.DragEvent<HTMLElement>, folderId: string | null) {
        event.preventDefault();
        const postId = event.dataTransfer.getData("text/plain") || draggingPostId;
        setDraggingPostId(null);
        setDropTargetFolderId(null);
        if (postId) await movePost(postId, folderId);
    }

    function postsInFolder(folderId: string | null): CmsPost[] {
        const folderIds = new Set(folders.map(folder => folder.id));
        return posts.filter(post => folderId === null
            ? post.folderId === undefined || !folderIds.has(post.folderId)
            : post.folderId === folderId);
    }

    function isFolderExpanded(folderId: string | null): boolean {
        return !collapsedFolderIds.includes(folderId ?? "root");
    }

    function toggleFolder(folderId: string | null) {
        const id = folderId ?? "root";
        setCollapsedFolderIds(current => current.includes(id)
            ? current.filter(candidate => candidate !== id)
            : [...current, id]);
    }

    function renderPost(post: CmsPost) {
        return (
            <button
                key={post.id}
                draggable
                className={post.id === selected.id ? "active" : ""}
                onDragStart={event => startDraggingPost(event, post.id)}
                onDragEnd={() => { setDraggingPostId(null); setDropTargetFolderId(null); }}
                onClick={() => { setSelected(post); setIsPostSelectorExpanded(false); }}
            >
                <span className="cms-post-details">
                    <span className="cms-post-title-row">
                        <span className="cms-post-title">{post.title}</span>
                        {post.type === "event"
                            ? <LuCalendarDays className="cms-post-type-icon" aria-label="Event" title="Event" />
                            : <LuFileText className="cms-post-type-icon" aria-label="Post" title="Post" />}
                    </span>
                    {post.isDraft && <small>Draft</small>}
                </span>
            </button>
        );
    }

    function dismissSelectorFromEditor(event: React.MouseEvent<HTMLDivElement>) {
        if (!isPostSelectorExpanded) return;

        event.preventDefault();
        event.stopPropagation();
        setIsPostSelectorExpanded(false);
    }

    return (
        <div className="cms-workspace">
            <aside className="cms-sidebar">
                <div>
                    <p className="cms-sidebar-brand">AKG CMS</p>
                    <nav>
                        {([ ["posts", "Posts & events"], ["files", "File management"], ["highlights", "Homepage highlights"] ] as [Section, string][]).map(([value, label]) => (
                            <button key={value} className={section === value ? "active" : ""} onClick={() => setSection(value)}>
                                {label}
                            </button>
                        ))}
                    </nav>
                </div>

                <div className="cms-sidebar-footer">
                    <div className="cms-theme-switcher" aria-label="Theme">
                        <button type="button" className={theme === "system" ? "btn btn-selected" : "btn"} onClick={() => setTheme("system")} aria-label="Use system theme" title="System theme"><LuMonitor /></button>
                        <button type="button" className={theme === "light" ? "btn btn-selected" : "btn"} onClick={() => setTheme("light")} aria-label="Use light theme" title="Light theme"><LuSunMedium /></button>
                        <button type="button" className={theme === "dark" ? "btn btn-selected" : "btn"} onClick={() => setTheme("dark")} aria-label="Use dark theme" title="Dark theme"><LuMoonStar /></button>
                    </div>
                    <div className="cms-user-box">
                        <div className="cms-user-info">
                            <div className="cms-user-label">Logged in as</div>
                            <div className="cms-user-name" title={email ?? "Administrator"}>{email ?? "Administrator"}</div>
                        </div>
                        <form action={signOutAction}>
                            <button className="cms-user-logout" type="submit">Logout</button>
                        </form>
                    </div>
                </div>
            </aside>

            <section className={`cms-workspace-main${isPostSelectorExpanded ? " selector-expanded" : ""}`}>
                {section === "files" ? <CmsMediaLibrary /> : section === "highlights" ? <CmsHighlightsManager posts={posts} /> : (
                    <>
                        <header className="cms-workspace-header">
                            <div>
                                <p className="cms-eyebrow">Content</p>
                                <h2>Posts and events</h2>
                            </div>
                            <div className="cms-workspace-actions">
                                <button className="cms-icon-action" onClick={() => void save()} title="Save draft" aria-label="Save draft"><LuSave /></button>
                                {selected.id && <button className="cms-icon-action cms-secondary-button" onClick={() => void publish()} title="Publish" aria-label="Publish"><LuUpload /></button>}
                            </div>
                        </header>

                        <div className={`cms-post-workspace${isPostSelectorExpanded ? " selector-expanded" : ""}`}>
                            <aside className="cms-post-selector">
                                <header className="cms-post-selector-header">
                                    <span>Posts</span>
                                    <div className="cms-post-selector-actions">
                                        <button className="cms-new-post-in-selector" type="button" onClick={() => { setSelected(emptyPost()); setTab("metadata"); setIsPostSelectorExpanded(false); }} aria-label="New post" title="New post"><LuPlus /></button>
                                        <button type="button" onClick={() => setIsPostSelectorExpanded(expanded => !expanded)} aria-label={isPostSelectorExpanded ? "Collapse post selector" : "Expand post selector"} title={isPostSelectorExpanded ? "Collapse post selector" : "Expand post selector"}>
                                            {isPostSelectorExpanded ? <LuPanelLeftClose /> : <LuPanelLeftOpen />}
                                        </button>
                                    </div>
                                </header>
                                <div className="cms-post-list">
                                    <div className="cms-folder-toolbar">
                                        <span>Folders</span>
                                        <button type="button" onClick={() => void createFolder()} title="New folder" aria-label="New folder"><LuFolderPlus /></button>
                                    </div>
                                    <section className={`cms-folder${dropTargetFolderId === null && draggingPostId !== null ? " drop-target" : ""}`} onDragOver={event => { event.preventDefault(); setDropTargetFolderId(null); }} onDrop={event => void dropPost(event, null)}>
                                        <header className="cms-folder-header">
                                            <button className="cms-folder-toggle" type="button" onClick={() => toggleFolder(null)} aria-expanded={isFolderExpanded(null)}>
                                                {isFolderExpanded(null) ? <LuChevronDown /> : <LuChevronRight />}<LuFolder /><span>Unsorted</span>
                                            </button>
                                        </header>
                                        {isFolderExpanded(null) && postsInFolder(null).map(post => renderPost(post))}
                                    </section>
                                    {folders.map(folder => (
                                        <section key={folder.id} className={`cms-folder${dropTargetFolderId === folder.id ? " drop-target" : ""}`} onDragOver={event => { event.preventDefault(); setDropTargetFolderId(folder.id); }} onDragLeave={() => setDropTargetFolderId(current => current === folder.id ? null : current)} onDrop={event => void dropPost(event, folder.id)}>
                                            <header className="cms-folder-header">
                                                <button className="cms-folder-toggle" type="button" onClick={() => toggleFolder(folder.id)} aria-expanded={isFolderExpanded(folder.id)}>
                                                    {isFolderExpanded(folder.id) ? <LuChevronDown /> : <LuChevronRight />}<LuFolder /><span className="cms-folder-name">{folder.name}</span>
                                                </button>
                                                <span className="cms-folder-actions">
                                                    <button type="button" onClick={() => void renameFolder(folder)} title="Rename folder" aria-label={`Rename ${folder.name}`}><LuPencil /></button>
                                                    <button type="button" onClick={() => void deleteFolder(folder)} title="Delete folder" aria-label={`Delete ${folder.name}`}><LuTrash2 /></button>
                                                </span>
                                            </header>
                                            {isFolderExpanded(folder.id) && postsInFolder(folder.id).map(post => renderPost(post))}
                                        </section>
                                    ))}
                                </div>
                            </aside>

                            <div className="cms-editor-area" onClickCapture={dismissSelectorFromEditor}>
                                <div className="cms-editor-tabs">
                                    <button className={tab === "metadata" ? "active" : ""} onClick={() => setTab("metadata")}>Metadata</button>
                                    <button className={tab === "mdx" ? "active" : ""} onClick={() => setTab("mdx")}>MDX</button>
                                </div>
                                {message && <p className="cms-editor-message">{message}</p>}
                                {tab === "metadata" ? (
                                    <div className="cms-form-grid">
                                        <label>Type<select value={selected.type} onChange={event => update("type", event.target.value)}><option value="post">Post</option><option value="event">Event</option></select></label>
                                        <label>Slug<input value={selected.id} onChange={event => update("id", event.target.value)} /></label>
                                        <label className="wide">Title<input value={selected.title} onChange={event => update("title", event.target.value)} /></label>
                                        <label className="wide">Short description<textarea value={selected.shortDescription} onChange={event => update("shortDescription", event.target.value)} /></label>
                                        {selected.type === "event" && <>
                                            <label>Start date<input value={selected.startDate ?? ""} onChange={event => update("startDate", event.target.value)} /></label>
                                            <label>Location<input value={selected.location ?? ""} onChange={event => update("location", event.target.value)} /></label>
                                        </>}
                                    </div>
                                ) : (
                                    <div className="cms-mdx-split">
                                        <MdxEditor value={selected.body} onChange={value => update("body", value)} />
                                        <div className="cms-preview">
                                            <div className="cms-preview-header"><span>Preview</span><span>Save draft to refresh</span></div>
                                            {selected.id ? <iframe key={previewKey} title="Post preview" src={`/cms/preview/${encodeURIComponent(selected.id)}`} /> : <p>Save the new draft to enable its preview.</p>}
                                        </div>
                                    </div>
                                )}
                            </div>
                        </div>
                    </>
                )}
            </section>
        </div>
    );
}
