import { NextRequest, NextResponse } from "next/server";
import { CmsPermissions, hasCmsPermission } from "../../../../content/cmsAuthorization";
import { createMediaFolder, deleteMediaFile, deleteMediaFolder, listMediaDirectory, moveMediaFile, renameMediaFile, uploadMediaFile } from "../../../../content/mediaStore";

export const runtime = "nodejs";

export async function GET(request: NextRequest) {
    if (!await hasCmsPermission(CmsPermissions.mediaManage))
        return NextResponse.json({ message: "Forbidden." }, { status: 403 });

    try {
        return NextResponse.json(await listMediaDirectory(request.nextUrl.searchParams.get("folder") ?? ""));
    }
    catch (error) {
        return NextResponse.json({ message: getMessage(error) }, { status: 400 });
    }
}

export async function POST(request: NextRequest) {
    if (!await hasCmsPermission(CmsPermissions.mediaManage))
        return NextResponse.json({ message: "Forbidden." }, { status: 403 });

    try {
        const formData = await request.formData();
        const folder = String(formData.get("folder") ?? "");
        const operation = String(formData.get("operation") ?? "upload");

        if (operation === "create-folder") {
            const name = String(formData.get("name") ?? "");
            return NextResponse.json(await createMediaFolder(folder, name), { status: 201 });
        }

        const file = formData.get("file");
        if (!(file instanceof File))
            return NextResponse.json({ message: "An image file is required." }, { status: 400 });

        return NextResponse.json(await uploadMediaFile(folder, file), { status: 201 });
    }
    catch (error) {
        return NextResponse.json({ message: getMessage(error) }, { status: 400 });
    }
}

export async function DELETE(request: NextRequest) {
    if (!await hasCmsPermission(CmsPermissions.mediaManage))
        return NextResponse.json({ message: "Forbidden." }, { status: 403 });

    try {
        const filePath = request.nextUrl.searchParams.get("path") ?? "";
        if (request.nextUrl.searchParams.get("kind") === "folder")
            await deleteMediaFolder(filePath);
        else
            await deleteMediaFile(filePath);
        return new NextResponse(null, { status: 204 });
    }
    catch (error) {
        return NextResponse.json({ message: getMessage(error) }, { status: 400 });
    }
}

export async function PUT(request: NextRequest) {
    if (!await hasCmsPermission(CmsPermissions.mediaManage))
        return NextResponse.json({ message: "Forbidden." }, { status: 403 });

    try {
        const { path, folder } = await request.json() as { path?: string; folder?: string };
        if (typeof path !== "string" || typeof folder !== "string")
            return NextResponse.json({ message: "An image path and target folder are required." }, { status: 400 });

        return NextResponse.json(await moveMediaFile(path, folder));
    }
    catch (error) {
        return NextResponse.json({ message: getMessage(error) }, { status: 400 });
    }
}

export async function PATCH(request: NextRequest) {
    if (!await hasCmsPermission(CmsPermissions.mediaManage))
        return NextResponse.json({ message: "Forbidden." }, { status: 403 });

    try {
        const { path, name } = await request.json() as { path?: string; name?: string };
        if (typeof path !== "string" || typeof name !== "string")
            return NextResponse.json({ message: "An image path and new file name are required." }, { status: 400 });

        return NextResponse.json(await renameMediaFile(path, name));
    }
    catch (error) {
        return NextResponse.json({ message: getMessage(error) }, { status: 400 });
    }
}

function getMessage(error: unknown): string {
    return error instanceof Error ? error.message : "The media operation failed.";
}
