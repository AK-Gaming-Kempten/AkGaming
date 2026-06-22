import { NextResponse } from "next/server";
import { readMediaFile } from "../../../content/mediaStore";

export const runtime = "nodejs";

type MediaRouteContext = {
    params: Promise<{ filePath: string[] }>;
};

export async function GET(_: Request, context: MediaRouteContext) {
    try {
        const { filePath } = await context.params;
        const media = await readMediaFile(filePath.join("/"));
        return new NextResponse(media.content, {
            headers: {
                "Cache-Control": "public, max-age=3600",
                "Content-Type": media.contentType,
            },
        });
    }
    catch {
        return new NextResponse(null, { status: 404 });
    }
}
