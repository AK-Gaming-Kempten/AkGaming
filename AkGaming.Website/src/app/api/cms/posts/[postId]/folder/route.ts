import { NextResponse } from "next/server";
import { isCmsAdministrator } from "../../../../../../content/cmsAuthorization";
import { movePostToFolder } from "../../../../../../content/postStore";

type MovePostFolderRouteContext = { params: Promise<{ postId: string }> };

export async function PUT(request: Request, context: MovePostFolderRouteContext) {
    if (!await isCmsAdministrator())
        return NextResponse.json({ message: "Forbidden." }, { status: 403 });

    try {
        const { postId } = await context.params;
        const { folderId } = await request.json() as { folderId?: string | null };
        if (folderId !== null && typeof folderId !== "string")
            return NextResponse.json({ message: "A folder ID or null is required." }, { status: 400 });

        return NextResponse.json(await movePostToFolder(postId, folderId));
    }
    catch (error) {
        return NextResponse.json({ message: error instanceof Error ? error.message : "The post could not be moved." }, { status: 400 });
    }
}
