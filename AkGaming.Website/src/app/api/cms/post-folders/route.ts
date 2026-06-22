import { NextResponse } from "next/server";
import { CmsPermissions, hasCmsPermission } from "../../../../content/cmsAuthorization";
import { createPostFolder, listPostFolders } from "../../../../content/postStore";

export async function GET() {
    if (!await hasCmsPermission(CmsPermissions.postsManage))
        return NextResponse.json({ message: "Forbidden." }, { status: 403 });

    return NextResponse.json(await listPostFolders());
}

export async function POST(request: Request) {
    if (!await hasCmsPermission(CmsPermissions.postsManage))
        return NextResponse.json({ message: "Forbidden." }, { status: 403 });

    try {
        const { name } = await request.json() as { name?: string };
        return NextResponse.json(await createPostFolder(name ?? ""), { status: 201 });
    }
    catch (error) {
        return NextResponse.json({ message: error instanceof Error ? error.message : "The folder could not be created." }, { status: 400 });
    }
}
