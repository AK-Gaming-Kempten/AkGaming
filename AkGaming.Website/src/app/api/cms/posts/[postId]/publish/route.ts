import { NextResponse } from "next/server";
import { isCmsAdministrator } from "../../../../../../content/cmsAuthorization";
import { publishDraft } from "../../../../../../content/postStore";

type PublishRouteContext = { params: Promise<{ postId: string }> };

export async function POST(_request: Request, context: PublishRouteContext) {
    if (!await isCmsAdministrator())
        return NextResponse.json({ message: "Forbidden." }, { status: 403 });

    try {
        const { postId } = await context.params;
        return NextResponse.json(await publishDraft(postId));
    }
    catch (error) {
        return NextResponse.json({ message: error instanceof Error ? error.message : "The draft could not be published." }, { status: 400 });
    }
}
