import { NextResponse } from "next/server";
import { CmsPermissions, hasCmsPermission } from "../../../../content/cmsAuthorization";
import { listHighlights, saveHighlights, type ContentHighlight } from "../../../../content/highlightStore";

export async function GET() {
    if (!await hasCmsPermission(CmsPermissions.highlightsManage))
        return NextResponse.json({ message: "Forbidden." }, { status: 403 });

    return NextResponse.json(await listHighlights());
}

export async function PUT(request: Request) {
    if (!await hasCmsPermission(CmsPermissions.highlightsManage))
        return NextResponse.json({ message: "Forbidden." }, { status: 403 });

    try {
        const highlights = await request.json() as ContentHighlight[];
        return NextResponse.json(await saveHighlights(highlights));
    }
    catch (error) {
        return NextResponse.json({ message: error instanceof Error ? error.message : "Highlights could not be saved." }, { status: 400 });
    }
}
