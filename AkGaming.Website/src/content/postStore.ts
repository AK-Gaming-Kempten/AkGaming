import "server-only";

import { promises as fs } from "node:fs";
import path from "node:path";
import frontMatter from "front-matter";

export type ContentKind = "post" | "event";

export type PostFrontMatter = {
    type?: ContentKind;
    id: string;
    title: string;
    shortDescription: string;
    startDate?: string;
    endDate?: string;
    location?: string;
    locationUrl?: string;
    folderId?: string;
};

export type PostFolder = {
    id: string;
    name: string;
};

type PostFoldersDocument = {
    folders: PostFolder[];
    assignments: Record<string, string | null>;
};

export type ContentPost = PostFrontMatter & {
    type: ContentKind;
    body: string;
    isDraft: boolean;
    updatedAt: string;
};

const validPostId = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;

export async function listPublishedPosts(): Promise<ContentPost[]> {
    const posts = await listPosts(getPublishedPostsDirectory(), false);
    return applyFolderAssignments(posts);
}

export async function listCmsPosts(): Promise<ContentPost[]> {
    const [publishedPosts, draftPosts] = await Promise.all([
        listPosts(getPublishedPostsDirectory(), false),
        listPosts(getDraftPostsDirectory(), true),
    ]);

    const draftsById = new Map(draftPosts.map(post => [post.id, post]));
    const merged = publishedPosts.map(post => draftsById.get(post.id) ?? post);
    for (const draft of draftPosts) {
        if (!merged.some(post => post.id === draft.id))
            merged.push(draft);
    }

    return (await applyFolderAssignments(merged)).sort((left, right) => left.title.localeCompare(right.title));
}

export async function getPublishedPost(id: string): Promise<ContentPost | null> {
    return withFolderAssignment(await findPost(getPublishedPostsDirectory(), id, false));
}

export async function getEditablePost(id: string): Promise<ContentPost | null> {
    const draft = await findPost(getDraftPostsDirectory(), id, true);
    return withFolderAssignment(draft ?? await findPost(getPublishedPostsDirectory(), id, false));
}

export async function listPostFolders(): Promise<PostFolder[]> {
    const document = await readPostFoldersDocument();
    return document.folders.sort((left, right) => left.name.localeCompare(right.name));
}

export async function createPostFolder(name: string): Promise<PostFolder> {
    const normalizedName = name.trim();
    if (!normalizedName)
        throw new Error("A folder name is required.");

    const document = await readPostFoldersDocument();
    const folders = document.folders;
    const baseId = toFolderId(normalizedName);
    if (!baseId)
        throw new Error("Folder names must contain letters or numbers.");

    let id = baseId;
    let suffix = 2;
    while (folders.some(folder => folder.id === id)) {
        id = `${baseId}-${suffix}`;
        suffix += 1;
    }

    const folder = { id, name: normalizedName };
    await writePostFolders({ folders: [...folders, folder], assignments: document.assignments });
    return folder;
}

export async function renamePostFolder(id: string, name: string): Promise<PostFolder> {
    validateFolderId(id);
    const normalizedName = name.trim();
    if (!normalizedName)
        throw new Error("A folder name is required.");

    const document = await readPostFoldersDocument();
    const folders = document.folders;
    const index = folders.findIndex(folder => folder.id === id);
    if (index === -1)
        throw new Error(`Folder '${id}' does not exist.`);

    const folder = { id, name: normalizedName };
    folders[index] = folder;
    await writePostFolders({ folders, assignments: document.assignments });
    return folder;
}

export async function deletePostFolder(id: string): Promise<void> {
    validateFolderId(id);
    const document = await readPostFoldersDocument();
    if (!document.folders.some(folder => folder.id === id))
        throw new Error(`Folder '${id}' does not exist.`);

    const assignments = Object.fromEntries(Object.entries(document.assignments).map(([postId, folderId]) => [postId, folderId === id ? null : folderId]));
    await writePostFolders({ folders: document.folders.filter(folder => folder.id !== id), assignments });
}

export async function movePostToFolder(postId: string, folderId: string | null): Promise<ContentPost> {
    validatePostId(postId);
    const post = await getEditablePost(postId);
    if (post === null)
        throw new Error(`Post '${postId}' does not exist.`);

    const document = await readPostFoldersDocument();
    if (folderId !== null) {
        validateFolderId(folderId);
        if (!document.folders.some(folder => folder.id === folderId))
            throw new Error(`Folder '${folderId}' does not exist.`);
    }

    const assignments = { ...document.assignments };
    assignments[postId] = folderId;

    await writePostFolders({ folders: document.folders, assignments });
    return { ...post, folderId: folderId ?? undefined };
}

export async function saveDraft(post: Omit<ContentPost, "isDraft" | "updatedAt">): Promise<ContentPost> {
    validatePost(post);

    if (post.folderId !== undefined)
        await assignPostFolder(post.id, post.folderId);

    const directory = getDraftPostsDirectory();
    await fs.mkdir(directory, { recursive: true });
    const filePath = path.join(directory, `${post.id}.mdx`);
    await writeFileAtomically(filePath, serializePost(post));
    return (await withFolderAssignment(await findPost(directory, post.id, true)))!;
}

export async function publishDraft(id: string): Promise<ContentPost> {
    validatePostId(id);

    const draft = await findPost(getDraftPostsDirectory(), id, true);
    if (draft === null)
        throw new Error(`No draft exists for post '${id}'.`);

    const directory = getPublishedPostsDirectory();
    await fs.mkdir(directory, { recursive: true });
    await removePostFiles(directory, id);
    const filePath = path.join(directory, `${id}.mdx`);
    await writeFileAtomically(filePath, serializePost(draft));
    await removePostFiles(getDraftPostsDirectory(), id);
    return (await withFolderAssignment(await findPost(directory, id, false)))!;
}

function getContentRoot(): string {
    const configuredRoot = process.env.AKG_WEBSITE_CONTENT_ROOT;
    if (!configuredRoot)
        return path.join(process.cwd(), "src", "data");

    return path.resolve(configuredRoot);
}

function getPublishedPostsDirectory(): string {
    return path.join(getContentRoot(), "posts");
}

function getDraftPostsDirectory(): string {
    return path.join(getContentRoot(), "drafts", "posts");
}

async function listPosts(directory: string, isDraft: boolean): Promise<ContentPost[]> {
    let entries: string[];
    try {
        entries = await fs.readdir(directory);
    }
    catch (error) {
        if (isMissingPath(error))
            return [];

        throw error;
    }

    const postFiles = entries.filter(isPostFile).sort((left, right) => left.localeCompare(right));
    const posts = await Promise.all(postFiles.map(fileName => readPost(path.join(directory, fileName), isDraft)));
    return posts.sort((left, right) => left.title.localeCompare(right.title));
}

async function findPost(directory: string, id: string, isDraft: boolean): Promise<ContentPost | null> {
    validatePostId(id);

    for (const extension of [".mdx", ".md"]) {
        const filePath = path.join(directory, `${id}${extension}`);
        try {
            return await readPost(filePath, isDraft);
        }
        catch (error) {
            if (isMissingPath(error))
                continue;

            throw error;
        }
    }

    return null;
}

async function readPost(filePath: string, isDraft: boolean): Promise<ContentPost> {
    const source = await fs.readFile(filePath, "utf8");
    const parsed = frontMatter<PostFrontMatter>(source);
    const attributes = parsed.attributes;
    validatePost({ ...attributes, body: parsed.body, type: attributes.type ?? "post" });
    const statistics = await fs.stat(filePath);

    return {
        ...attributes,
        type: attributes.type ?? "post",
        body: parsed.body.trim(),
        isDraft,
        updatedAt: statistics.mtime.toISOString(),
    };
}

function serializePost(post: Omit<ContentPost, "isDraft" | "updatedAt">): string {
    const frontMatter = {
        type: post.type,
        id: post.id,
        title: post.title,
        shortDescription: post.shortDescription,
        ...(post.type === "event" ? {
            startDate: post.startDate,
            ...(post.endDate ? { endDate: post.endDate } : {}),
            location: post.location,
            ...(post.locationUrl ? { locationUrl: post.locationUrl } : {}),
        } : {}),
    };

    return `---\n${Object.entries(frontMatter)
        .filter(([, value]) => value !== undefined && value !== "")
        .map(([key, value]) => `${key}: ${JSON.stringify(value)}`)
        .join("\n")}\n---\n\n${post.body.trim()}\n`;
}

async function removePostFiles(directory: string, id: string): Promise<void> {
    await Promise.all([".mdx", ".md"].map(async extension => {
        try {
            await fs.unlink(path.join(directory, `${id}${extension}`));
        }
        catch (error) {
            if (!isMissingPath(error))
                throw error;
        }
    }));
}

async function writeFileAtomically(filePath: string, source: string): Promise<void> {
    const temporaryPath = `${filePath}.${process.pid}.${Date.now()}.tmp`;
    await fs.writeFile(temporaryPath, source, "utf8");
    await fs.rename(temporaryPath, filePath);
}

function validatePost(post: Omit<ContentPost, "isDraft" | "updatedAt">): void {
    validatePostId(post.id);
    if (!post.title.trim())
        throw new Error("A post title is required.");
    if (!post.shortDescription.trim())
        throw new Error("A short description is required.");
    if (post.type !== "post" && post.type !== "event")
        throw new Error("Post type must be either 'post' or 'event'.");
    if (post.type === "event" && (!post.startDate || !post.location))
        throw new Error("Events require a start date and location.");
    if (post.folderId !== undefined)
        validateFolderId(post.folderId);
}

function validatePostId(id: string): void {
    if (!validPostId.test(id))
        throw new Error("Post IDs must use lowercase letters, numbers, and hyphens only.");
}

function getPostFoldersFilePath(): string {
    return path.join(getContentRoot(), "post-folders.json");
}

async function readPostFoldersDocument(): Promise<PostFoldersDocument> {
    try {
        const source = await fs.readFile(getPostFoldersFilePath(), "utf8");
        const parsed = JSON.parse(source) as unknown;
        if (Array.isArray(parsed))
            return { folders: parsed as PostFolder[], assignments: {} };

        if (typeof parsed === "object" && parsed !== null && "folders" in parsed) {
            const document = parsed as Partial<PostFoldersDocument>;
            return {
                folders: Array.isArray(document.folders) ? document.folders : [],
                assignments: document.assignments ?? {},
            };
        }

        return { folders: [], assignments: {} };
    }
    catch (error) {
        if (isMissingPath(error))
            return { folders: [], assignments: {} };

        throw error;
    }
}

async function writePostFolders(document: PostFoldersDocument): Promise<void> {
    await fs.mkdir(getContentRoot(), { recursive: true });
    await writeFileAtomically(getPostFoldersFilePath(), `${JSON.stringify(document, null, 2)}\n`);
}

async function applyFolderAssignments(posts: ContentPost[]): Promise<ContentPost[]> {
    const document = await readPostFoldersDocument();
    return posts.map(post => ({
        ...post,
        folderId: Object.hasOwn(document.assignments, post.id)
            ? document.assignments[post.id] ?? undefined
            : post.folderId,
    }));
}

async function withFolderAssignment(post: ContentPost | null): Promise<ContentPost | null> {
    if (post === null)
        return null;

    const [assignedPost] = await applyFolderAssignments([post]);
    return assignedPost;
}

async function assignPostFolder(postId: string, folderId: string): Promise<void> {
    const document = await readPostFoldersDocument();
    if (!document.folders.some(folder => folder.id === folderId))
        throw new Error(`Folder '${folderId}' does not exist.`);

    await writePostFolders({
        folders: document.folders,
        assignments: { ...document.assignments, [postId]: folderId },
    });
}

function toFolderId(name: string): string {
    return name
        .toLocaleLowerCase()
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .replace(/[^a-z0-9]+/g, "-")
        .replace(/^-+|-+$/g, "");
}

function validateFolderId(id: string): void {
    if (!validPostId.test(id))
        throw new Error("Folder IDs must use lowercase letters, numbers, and hyphens only.");
}

function isPostFile(fileName: string): boolean {
    return fileName.endsWith(".md") || fileName.endsWith(".mdx");
}

function isMissingPath(error: unknown): boolean {
    return typeof error === "object" && error !== null && "code" in error && error.code === "ENOENT";
}
