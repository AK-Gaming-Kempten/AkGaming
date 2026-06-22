import { NextResponse } from "next/server";
import { CmsPermissions, hasCmsPermission } from "../../../../../content/cmsAuthorization";
import { deletePostFolder, renamePostFolder } from "../../../../../content/postStore";

type FolderRouteContext = { params: Promise<{ folderId: string }> };

export async function PATCH(request: Request, context: FolderRouteContext) {
    if (!await hasCmsPermission(CmsPermissions.postsManage))
        return NextResponse.json({ message: "Forbidden." }, { status: 403 });

    try {
        const { folderId } = await context.params;
        const { name } = await request.json() as { name?: string };
        return NextResponse.json(await renamePostFolder(folderId, name ?? ""));
    }
    catch (error) {
        return NextResponse.json({ message: error instanceof Error ? error.message : "The folder could not be renamed." }, { status: 400 });
    }
}

export async function DELETE(_request: Request, context: FolderRouteContext) {
    if (!await hasCmsPermission(CmsPermissions.postsManage))
        return NextResponse.json({ message: "Forbidden." }, { status: 403 });

    try {
        const { folderId } = await context.params;
        await deletePostFolder(folderId);
        return new NextResponse(null, { status: 204 });
    }
    catch (error) {
        return NextResponse.json({ message: error instanceof Error ? error.message : "The folder could not be deleted." }, { status: 400 });
    }
}
