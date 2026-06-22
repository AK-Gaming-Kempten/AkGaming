import "server-only";

import { promises as fs } from "node:fs";
import path from "node:path";

const supportedExtensions = new Set([".jpg", ".jpeg", ".png", ".webp", ".avif", ".gif"]);
const maximumUploadSize = 15 * 1024 * 1024;
const mediaContentTypes: Record<string, string> = {
    ".avif": "image/avif",
    ".gif": "image/gif",
    ".jpeg": "image/jpeg",
    ".jpg": "image/jpeg",
    ".png": "image/png",
    ".svg": "image/svg+xml",
    ".webp": "image/webp",
};

export type CmsMediaFile = {
    name: string;
    path: string;
    url: string;
    size: number;
};

export type CmsMediaFolder = {
    name: string;
    path: string;
};

export type CmsMediaDirectory = {
    folder: string;
    folders: CmsMediaFolder[];
    files: CmsMediaFile[];
};

export async function listMediaDirectory(folder: string): Promise<CmsMediaDirectory> {
    const normalizedFolder = normalizeRelativePath(folder);
    const directory = resolveMediaPath(normalizedFolder);
    await fs.mkdir(directory, { recursive: true });

    const entries = await fs.readdir(directory, { withFileTypes: true });
    const folders = entries
        .filter(entry => entry.isDirectory())
        .map(entry => ({ name: entry.name, path: joinMediaPath(normalizedFolder, entry.name) }))
        .sort((left, right) => left.name.localeCompare(right.name));
    const fileEntries = entries.filter(entry => entry.isFile() && supportedExtensions.has(path.extname(entry.name).toLowerCase()));
    const files = await Promise.all(fileEntries.map(async entry => {
        const relativePath = joinMediaPath(normalizedFolder, entry.name);
        const statistics = await fs.stat(resolveMediaPath(relativePath));
        return {
            name: entry.name,
            path: relativePath,
            url: `/media/${relativePath}`,
            size: statistics.size,
        };
    }));

    return { folder: normalizedFolder, folders, files: files.sort((left, right) => left.name.localeCompare(right.name)) };
}

export async function createMediaFolder(folder: string, name: string): Promise<CmsMediaFolder> {
    const normalizedFolder = normalizeRelativePath(folder);
    const folderName = normalizeFolderName(name);
    if (!folderName)
        throw new Error("A folder name is required.");

    const relativePath = joinMediaPath(normalizedFolder, folderName);
    await fs.mkdir(resolveMediaPath(relativePath), { recursive: false });
    return { name: folderName, path: relativePath };
}

export async function uploadMediaFile(folder: string, file: File): Promise<CmsMediaFile> {
    const normalizedFolder = normalizeRelativePath(folder);
    const extension = path.extname(file.name).toLowerCase();
    if (!supportedExtensions.has(extension))
        throw new Error("Only JPG, PNG, WebP, AVIF, and GIF images can be uploaded.");
    if (file.size > maximumUploadSize)
        throw new Error("Images must not exceed 15 MB.");

    const directory = resolveMediaPath(normalizedFolder);
    await fs.mkdir(directory, { recursive: true });
    const name = await getAvailableFileName(directory, sanitizeFileName(file.name));
    const relativePath = joinMediaPath(normalizedFolder, name);
    await fs.writeFile(resolveMediaPath(relativePath), Buffer.from(await file.arrayBuffer()));

    return { name, path: relativePath, url: `/media/${relativePath}`, size: file.size };
}

export async function readMediaFile(filePath: string): Promise<{ content: ArrayBuffer; contentType: string }> {
    const normalizedPath = normalizeRelativePath(filePath);
    const extension = path.extname(normalizedPath).toLowerCase();
    const contentType = mediaContentTypes[extension];
    if (contentType === undefined)
        throw new Error("Unsupported media file type.");

    const source = await fs.readFile(resolveMediaPath(normalizedPath));
    const content = new Uint8Array(source.byteLength);
    content.set(source);
    return { content: content.buffer, contentType };
}

export async function deleteMediaFile(filePath: string): Promise<void> {
    const normalizedPath = normalizeRelativePath(filePath);
    if (!supportedExtensions.has(path.extname(normalizedPath).toLowerCase()))
        throw new Error("Only image files in the media library can be deleted.");

    await fs.unlink(resolveMediaPath(normalizedPath));
}

export async function deleteMediaFolder(folder: string): Promise<void> {
    const normalizedFolder = normalizeRelativePath(folder);
    if (!normalizedFolder)
        throw new Error("The root media folder cannot be deleted.");

    try {
        await fs.rmdir(resolveMediaPath(normalizedFolder));
    }
    catch (error) {
        if (isNonEmptyDirectory(error))
            throw new Error("Folders can only be deleted after their files and subfolders have been removed or moved.");

        throw error;
    }
}

export async function moveMediaFile(filePath: string, folder: string): Promise<CmsMediaFile> {
    const normalizedFilePath = normalizeRelativePath(filePath);
    const normalizedFolder = normalizeRelativePath(folder);
    if (!supportedExtensions.has(path.extname(normalizedFilePath).toLowerCase()))
        throw new Error("Only image files in the media library can be moved.");

    const sourcePath = resolveMediaPath(normalizedFilePath);
    const targetDirectory = resolveMediaPath(normalizedFolder);
    const sourceName = path.basename(normalizedFilePath);
    const sourceFolder = path.posix.dirname(normalizedFilePath) === "." ? "" : path.posix.dirname(normalizedFilePath);
    if (sourceFolder === normalizedFolder) {
        const statistics = await fs.stat(sourcePath);
        return { name: sourceName, path: normalizedFilePath, url: `/media/${normalizedFilePath}`, size: statistics.size };
    }

    await fs.mkdir(targetDirectory, { recursive: true });
    const name = await getAvailableFileName(targetDirectory, sourceName);
    const targetPath = joinMediaPath(normalizedFolder, name);
    await fs.rename(sourcePath, resolveMediaPath(targetPath));
    const statistics = await fs.stat(resolveMediaPath(targetPath));
    return { name, path: targetPath, url: `/media/${targetPath}`, size: statistics.size };
}

function getMediaRoot(): string {
    return path.join(process.cwd(), "public", "media");
}

function resolveMediaPath(relativePath: string): string {
    const root = getMediaRoot();
    const resolved = path.resolve(root, relativePath);
    if (resolved !== root && !resolved.startsWith(`${root}${path.sep}`))
        throw new Error("Invalid media path.");

    return resolved;
}

function normalizeRelativePath(value: string): string {
    const segments = value.replaceAll("\\", "/").split("/").filter(Boolean);
    if (segments.some(segment => segment === "." || segment === ".."))
        throw new Error("Invalid media path.");

    return segments.join("/");
}

function normalizeFolderName(value: string): string {
    const name = value.trim().replaceAll("\\", "/");
    if (!name || name.includes("/") || name === "." || name === "..")
        return "";

    return name.replace(/[^a-zA-Z0-9._-]/g, "-");
}

function joinMediaPath(parent: string, child: string): string {
    return parent ? `${parent}/${child}` : child;
}

function sanitizeFileName(name: string): string {
    const extension = path.extname(name).toLowerCase();
    const baseName = path.basename(name, extension)
        .replace(/[^a-zA-Z0-9._-]/g, "-")
        .replace(/-+/g, "-")
        .replace(/^-+|-+$/g, "") || "image";
    return `${baseName}${extension}`;
}

async function getAvailableFileName(directory: string, initialName: string): Promise<string> {
    const extension = path.extname(initialName);
    const baseName = path.basename(initialName, extension);
    let name = initialName;
    let suffix = 2;

    while (await exists(path.join(directory, name))) {
        name = `${baseName}-${suffix}${extension}`;
        suffix += 1;
    }

    return name;
}

async function exists(filePath: string): Promise<boolean> {
    try {
        await fs.access(filePath);
        return true;
    }
    catch {
        return false;
    }
}

function isNonEmptyDirectory(error: unknown): boolean {
    return typeof error === "object" && error !== null && "code" in error && (error.code === "ENOTEMPTY" || error.code === "EEXIST");
}
