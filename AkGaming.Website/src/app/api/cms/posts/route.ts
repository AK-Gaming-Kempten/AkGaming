import { NextResponse } from "next/server";
import { CmsPermissions, hasCmsPermission } from "../../../../content/cmsAuthorization";
import { listCmsPosts, saveDraft, type ContentPost } from "../../../../content/postStore";

export async function GET() {
    if (!await hasCmsPermission(CmsPermissions.postsManage) && !await hasCmsPermission(CmsPermissions.highlightsManage))
        return NextResponse.json({ message: "Forbidden." }, { status: 403 });

    return NextResponse.json(await listCmsPosts());
}

export async function POST(request: Request) {
    if (!await hasCmsPermission(CmsPermissions.postsManage))
        return NextResponse.json({ message: "Forbidden." }, { status: 403 });

    try {
        const post = await request.json() as Omit<ContentPost, "isDraft" | "updatedAt">;
        return NextResponse.json(await saveDraft(post), { status: 201 });
    }
    catch (error) {
        return NextResponse.json({ message: getMessage(error) }, { status: 400 });
    }
}

function getMessage(error: unknown): string {
    return error instanceof Error ? error.message : "The draft could not be saved.";
}
